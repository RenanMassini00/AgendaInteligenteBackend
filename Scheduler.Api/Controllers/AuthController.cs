using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Scheduler.Api.Data;
using Scheduler.Api.DTOs;
using Scheduler.Api.Entities;

namespace Scheduler.Api.Controllers;

[ApiController]
[Route("api/auth")]
[EnableRateLimiting("Auth")]
public class AuthController : ControllerBase
{
    private readonly AppDbContext _context;

    public AuthController(AppDbContext context)
    {
        _context = context;
    }

    [HttpPost("login")]
    public async Task<ActionResult<LoginResponse>> Login(LoginRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
            return BadRequest(new ApiMessage("E-mail e senha são obrigatórios."));

        var normalizedEmail = request.Email.Trim().ToLowerInvariant();
        var user = await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Email == normalizedEmail && x.IsActive);

        if (user is null)
            return Unauthorized(new ApiMessage("Usuário não encontrado ou inativo."));

        if (!string.Equals(user.PasswordHash, request.Password, StringComparison.Ordinal))
            return Unauthorized(new ApiMessage("Senha inválida."));

        var normalizedRole = NormalizeRole(user.Role);
        var token = $"dev-token-{normalizedRole}-user-{user.Id}";

        return Ok(new LoginResponse(token, await ToUserResponseAsync(user)));
    }

    [HttpPost("register-professional")]
    public async Task<ActionResult<LoginResponse>> RegisterProfessional(RegisterProfessionalRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.FullName) ||
            string.IsNullOrWhiteSpace(request.Email) ||
            string.IsNullOrWhiteSpace(request.Password))
        {
            return BadRequest(new ApiMessage("Nome, e-mail e senha são obrigatórios."));
        }

        if (!request.HasAppointmentsModule && !request.HasCatalogModule)
        {
            return BadRequest(new ApiMessage("Selecione pelo menos um módulo: agendamentos ou catálogo."));
        }

        var email = request.Email.Trim().ToLowerInvariant();
        var exists = await _context.Users.AnyAsync(x => x.Email == email);

        if (exists)
        {
            return Conflict(new ApiMessage("Já existe uma conta com esse e-mail."));
        }

        var generatedSlug = string.IsNullOrWhiteSpace(request.PublicSlug)
            ? GenerateSlug(string.IsNullOrWhiteSpace(request.BusinessName) ? request.FullName : request.BusinessName!)
            : GenerateSlug(request.PublicSlug);

        var user = new User
        {
            FullName = request.FullName.Trim(),
            BusinessName = string.IsNullOrWhiteSpace(request.BusinessName)
                ? request.FullName.Trim()
                : request.BusinessName.Trim(),
            Email = email,
            Phone = request.Phone?.Trim(),
            PasswordHash = request.Password,
            Specialty = request.Specialty?.Trim(),
            Role = "professional",
            PublicSlug = generatedSlug,
            Timezone = string.IsNullOrWhiteSpace(request.Timezone)
                ? "America/Sao_Paulo"
                : request.Timezone.Trim(),
            HasAppointmentsModule = request.HasAppointmentsModule,
            HasCatalogModule = request.HasCatalogModule,
            IsActive = true,
            CreatedAt = DateTime.Now,
            UpdatedAt = DateTime.Now
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        _context.UserSettings.Add(new UserSetting
        {
            UserId = user.Id,
            ThemeMode = "light",
            AccentColor = "blue",
            LogoUrl = null,
            LanguageCode = "pt-BR",
            ReminderMinutes = 60,
            EmailNotifications = false,
            WhatsAppNotifications = true,
            CreatedAt = DateTime.Now,
            UpdatedAt = DateTime.Now
        });

        await _context.SaveChangesAsync();

        return Ok(new LoginResponse(
            $"dev-token-professional-user-{user.Id}",
            await ToUserResponseAsync(user)
        ));
    }

    [HttpPost("register-client")]
    public async Task<ActionResult<LoginResponse>> RegisterClient(RegisterClientRequest request)
    {
        if (request.ProfessionalUserId == 0 || string.IsNullOrWhiteSpace(request.FullName) || string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Phone) || string.IsNullOrWhiteSpace(request.Password))
            return BadRequest(new ApiMessage("Profissional, nome, e-mail, telefone e senha são obrigatórios."));

        var professional = await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == request.ProfessionalUserId && x.Role == "professional" && x.IsActive);

        if (professional is null) return BadRequest(new ApiMessage("Profissional inválido."));

        var email = request.Email.Trim().ToLowerInvariant();
        var exists = await _context.Users.AnyAsync(x => x.Email == email);
        if (exists) return Conflict(new ApiMessage("Já existe uma conta com esse e-mail."));

        var client = new Client
        {
            UserId = request.ProfessionalUserId,
            FullName = request.FullName.Trim(),
            Email = email,
            Phone = request.Phone.Trim(),
            BirthDate = request.BirthDate,
            Notes = request.Notes,
            IsActive = true,
            CreatedAt = DateTime.Now,
            UpdatedAt = DateTime.Now
        };

        _context.Clients.Add(client);
        await _context.SaveChangesAsync();

        var user = new User
        {
            FullName = client.FullName,
            Email = email,
            Phone = client.Phone,
            PasswordHash = request.Password,
            Role = "client",
            ProfessionalUserId = request.ProfessionalUserId,
            ClientId = client.Id,
            IsActive = true,
            CreatedAt = DateTime.Now,
            UpdatedAt = DateTime.Now
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        return Ok(new LoginResponse($"dev-token-client-user-{user.Id}", await ToUserResponseAsync(user)));
    }

    [HttpGet("me")]
    public async Task<ActionResult<UserResponse>> Me([FromQuery] ulong userId = 1)
    {
        var user = await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == userId && x.IsActive);

        if (user is null) return NotFound(new ApiMessage("Usuário não encontrado."));

        return Ok(await ToUserResponseAsync(user));
    }

    private static string NormalizeRole(string role)
    {
        var normalized = (role ?? string.Empty).Trim().ToLowerInvariant();

        return normalized switch
        {
            "master admin" => "master_admin",
            "master_admin" => "master_admin",
            "client" => "client",
            _ => "professional"
        };
    }

    private async Task<UserResponse> ToUserResponseAsync(User user)
    {
        var normalizedRole = NormalizeRole(user.Role);
        var brandingUserId = normalizedRole == "client" && user.ProfessionalUserId.HasValue
            ? user.ProfessionalUserId.Value
            : user.Id;

        var settings = await _context.UserSettings
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.UserId == brandingUserId);

        return new UserResponse(
            Id: user.Id,
            FullName: user.FullName,
            BusinessName: user.BusinessName,
            Email: user.Email,
            Phone: user.Phone,
            Specialty: user.Specialty,
            Timezone: user.Timezone,
            Role: normalizedRole,
            PublicSlug: user.PublicSlug,
            ProfessionalUserId: user.ProfessionalUserId,
            ClientId: user.ClientId,
            HasAppointmentsModule: user.HasAppointmentsModule,
            HasCatalogModule: user.HasCatalogModule,
            ThemeMode: settings?.ThemeMode ?? "light",
            AccentColor: settings?.AccentColor ?? "blue",
            LogoUrl: settings?.LogoUrl
        );
    }

    private static string GenerateSlug(string raw)
    {
        var sanitized = new string(raw
            .Trim()
            .ToLowerInvariant()
            .Select(c => char.IsLetterOrDigit(c) ? c : '-')
            .ToArray())
            .Trim('-');

        while (sanitized.Contains("--")) sanitized = sanitized.Replace("--", "-");
        return string.IsNullOrWhiteSpace(sanitized) ? $"agenda-{Guid.NewGuid():N}"[..14] : sanitized;
    }
}
