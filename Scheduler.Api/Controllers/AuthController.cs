using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Scheduler.Api.Data;
using Scheduler.Api.DTOs;
using Scheduler.Api.Entities;

namespace Scheduler.Api.Controllers;

[ApiController]
[Route("api/auth")]
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

        var token = $"dev-token-{user.Role}-user-{user.Id}";
        return Ok(new LoginResponse(token, ToUserResponse(user)));
    }

    [HttpPost("register-professional")]
    public async Task<ActionResult<LoginResponse>> RegisterProfessional(RegisterProfessionalRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.FullName) || string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
            return BadRequest(new ApiMessage("Nome, e-mail e senha são obrigatórios."));

        var email = request.Email.Trim().ToLowerInvariant();
        var exists = await _context.Users.AnyAsync(x => x.Email == email);
        if (exists) return Conflict(new ApiMessage("Já existe uma conta com esse e-mail."));

        var user = new User
        {
            FullName = request.FullName.Trim(),
            BusinessName = string.IsNullOrWhiteSpace(request.BusinessName) ? request.FullName.Trim() : request.BusinessName.Trim(),
            Email = email,
            Phone = request.Phone?.Trim(),
            PasswordHash = request.Password,
            Specialty = request.Specialty?.Trim(),
            Role = "professional",
            PublicSlug = GenerateSlug(string.IsNullOrWhiteSpace(request.BusinessName) ? request.FullName : request.BusinessName!),
            IsActive = true,
            CreatedAt = DateTime.Now,
            UpdatedAt = DateTime.Now
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        _context.UserSettings.Add(new UserSetting
        {
            UserId = user.Id,
            Theme = "light",
            LanguageCode = "pt-BR",
            ReminderMinutes = 60,
            EmailNotifications = false,
            WhatsappNotifications = true,
            CreatedAt = DateTime.Now,
            UpdatedAt = DateTime.Now
        });
        await _context.SaveChangesAsync();

        return Ok(new LoginResponse($"dev-token-professional-user-{user.Id}", ToUserResponse(user)));
    }

    [HttpPost("register-client")]
    public async Task<ActionResult<LoginResponse>> RegisterClient(RegisterClientRequest request)
    {
        if (request.ProfessionalUserId == 0 || string.IsNullOrWhiteSpace(request.FullName) || string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Phone) || string.IsNullOrWhiteSpace(request.Password))
            return BadRequest(new ApiMessage("Profissional, nome, e-mail, telefone e senha são obrigatórios."));

        var professional = await _context.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Id == request.ProfessionalUserId && x.Role == "professional" && x.IsActive);
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

        return Ok(new LoginResponse($"dev-token-client-user-{user.Id}", ToUserResponse(user)));
    }

    [HttpGet("me")]
    public async Task<ActionResult<UserResponse>> Me([FromQuery] ulong userId = 1)
    {
        var user = await _context.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Id == userId && x.IsActive);
        if (user is null) return NotFound(new ApiMessage("Usuário não encontrado."));
        return Ok(ToUserResponse(user));
    }

    private static UserResponse ToUserResponse(User user) => new(
        user.Id,
        user.FullName,
        user.BusinessName,
        user.Email,
        user.Phone,
        user.Specialty,
        user.Timezone,
        user.Role,
        user.ProfessionalUserId,
        user.ClientId,
        user.PublicSlug
    );

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
