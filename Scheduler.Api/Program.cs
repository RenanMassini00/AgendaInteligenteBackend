using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Scheduler.Api.Data;
using Scheduler.Api.Options;
using Scheduler.Api.Services;
using Scheduler.Api.Services.Contracts;
using Scheduler.Api.Services.Notifications;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
        RateLimitPartition.GetFixedWindowLimiter(
            GetRateLimitPartitionKey(context),
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 600,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                AutoReplenishment = true,
            }));

    options.AddPolicy("Auth", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            $"auth:{GetRateLimitPartitionKey(context)}",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 20,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                AutoReplenishment = true,
            }));

    options.OnRejected = async (context, cancellationToken) =>
    {
        context.HttpContext.Response.ContentType = "application/json";
        await context.HttpContext.Response.WriteAsync(
            "{\"message\":\"Muitas requisições. Tente novamente em instantes.\"}",
            cancellationToken);
    };
});
builder.Services.Configure<ZApiOptions>(builder.Configuration.GetSection("ZApi"));
builder.Services.Configure<EmailOptions>(builder.Configuration.GetSection("Email"));
builder.Services.Configure<GoogleCalendarOptions>(builder.Configuration.GetSection("GoogleCalendar"));
builder.Services.Configure<WebPushOptions>(builder.Configuration.GetSection("WebPush"));

builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
    {
        policy
            .WithOrigins(
                "https://macroloapp.com.br",
                "https://www.macroloapp.com.br"
            )
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

builder.Services.Configure<WhatsAppMetaOptions>(
    builder.Configuration.GetSection("WhatsAppMeta")
);

builder.Services.AddHttpClient<IWhatsAppService, MetaWhatsAppService>();

builder.Services.AddDbContext<AppDbContext>(options =>
{
    options.UseMySql(connectionString, new MySqlServerVersion(new Version(8, 0, 0)));
});

builder.Services.Configure<WhatsAppOptions>(
    builder.Configuration.GetSection("WhatsApp"));

builder.Services.AddHttpClient<IWhatsAppGateway, HttpWhatsAppGateway>((sp, client) =>
{
    var options = sp.GetRequiredService<
        Microsoft.Extensions.Options.IOptions<WhatsAppOptions>>().Value;

    if (!string.IsNullOrWhiteSpace(options.BaseUrl))
    {
        client.BaseAddress = new Uri(options.BaseUrl);
    }
});

builder.Services.AddScoped<IAppointmentNotificationService, AppointmentNotificationService>();
builder.Services.AddHttpClient<IWhatsAppService, ZApiWhatsAppService>();
builder.Services.AddScoped<IEmailService, SmtpEmailService>();
builder.Services.AddScoped<IGoogleCalendarService, GoogleCalendarService>();
builder.Services.AddScoped<IPushNotificationService, WebPushNotificationService>();
builder.Services.AddScoped<IBookingAutomationService, BookingAutomationService>();

var app = builder.Build();

app.Use(async (context, next) =>
{
    if (IsSensitiveProbePath(context.Request.Path))
    {
        var logger = context.RequestServices
            .GetRequiredService<ILoggerFactory>()
            .CreateLogger("Security");

        logger.LogWarning(
            "Blocked sensitive path probe {Method} {Path} from {RemoteIp} with user-agent {UserAgent}",
            context.Request.Method,
            context.Request.Path,
            context.Connection.RemoteIpAddress,
            context.Request.Headers["User-Agent"].ToString());

        context.Response.StatusCode = StatusCodes.Status404NotFound;
        return;
    }

    await next();
});

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("Frontend");
app.UseRateLimiter();
app.UseAuthorization();
app.MapControllers();

app.MapGet("/", () => Results.Ok(new
{
    name = "Scheduler API",
    status = "running",
    swagger = app.Environment.IsDevelopment() ? "/swagger" : null
}));

app.Run();

static string GetRateLimitPartitionKey(HttpContext context)
{
    return context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
}

static bool IsSensitiveProbePath(PathString path)
{
    if (!path.HasValue)
    {
        return false;
    }

    var normalized = path.Value!.Replace('\\', '/').ToLowerInvariant();
    var segments = normalized.Split('/', StringSplitOptions.RemoveEmptyEntries);

    foreach (var segment in segments)
    {
        if (segment == ".well-known")
        {
            continue;
        }

        if (segment.StartsWith('.') ||
            segment is "web.config" or "dockerfile" or "docker-compose.yml" or "docker-compose.yaml" ||
            (segment.StartsWith("appsettings") && segment.EndsWith(".json")) ||
            segment.EndsWith(".pem") ||
            segment.EndsWith(".pfx") ||
            segment.EndsWith(".key"))
        {
            return true;
        }
    }

    return false;
}
