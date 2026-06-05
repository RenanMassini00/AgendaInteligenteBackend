using Microsoft.EntityFrameworkCore;
using Scheduler.Api.Data;
using Scheduler.Api.Options;
using Scheduler.Api.Services;
using Scheduler.Api.Services.Contracts;
using Scheduler.Api.Services.Notifications;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.Configure<ZApiOptions>(builder.Configuration.GetSection("ZApi"));
builder.Services.Configure<EmailOptions>(builder.Configuration.GetSection("Email"));
builder.Services.Configure<GoogleCalendarOptions>(builder.Configuration.GetSection("GoogleCalendar"));

builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
    {
        policy
            .WithOrigins(
                "https://macroloapp.com.br",
                "http://2.25.147.236:3000"
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
builder.Services.AddScoped<IBookingAutomationService, BookingAutomationService>();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.UseCors("Frontend");
app.UseAuthorization();
app.MapControllers();

app.MapGet("/", () => Results.Ok(new
{
    name = "Scheduler API",
    status = "running",
    swagger = "/swagger"
}));

app.Run();
