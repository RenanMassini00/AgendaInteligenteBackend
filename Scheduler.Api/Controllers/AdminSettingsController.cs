using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Scheduler.Api.Data;
using Scheduler.Api.DTOs;
using Scheduler.Api.Entities;

namespace Scheduler.Api.Controllers;

[ApiController]
[Route("api/admin/settings")]
public class AdminSettingsController : ControllerBase
{
    private readonly AppDbContext _context;

    private static readonly HashSet<string> AllowedThemes =
    [
        "light",
        "dark"
    ];

    private static readonly HashSet<string> AllowedAccentColors =
    [
        "blue",
        "pink",
        "violet",
        "emerald",
        "cyan",
        "amber",
        "rose",
        "slate"
    ];

    public AdminSettingsController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet("users")]
    public async Task<ActionResult<List<AdminBrandingUserResponse>>> GetUsers()
    {
        var users = await _context.Users
            .AsNoTracking()
            .Where(x => x.Role == "professional" && x.IsActive)
            .OrderBy(x => x.BusinessName)
            .ThenBy(x => x.FullName)
            .ToListAsync();

        var settings = await _context.UserSettings
            .AsNoTracking()
            .ToListAsync();

        var result = users.Select(user =>
        {
            var userSetting = settings.FirstOrDefault(x => x.UserId == user.Id);

            return new AdminBrandingUserResponse(
                user.Id,
                user.FullName,
                user.BusinessName,
                user.Email,
                userSetting?.ThemeMode ?? "light",
                userSetting?.AccentColor ?? "blue",
                userSetting?.LogoUrl
            );
        }).ToList();

        return Ok(result);
    }

    [HttpPut("users/{userId}")]
    public async Task<ActionResult<ApiMessage>> UpdateUserBranding(
        ulong userId,
        [FromBody] AdminBrandingUpdateRequest request)
    {
        var theme = (request.Theme ?? string.Empty).Trim().ToLowerInvariant();
        var accentColor = (request.AccentColor ?? string.Empty).Trim().ToLowerInvariant();

        if (!AllowedThemes.Contains(theme))
        {
            return BadRequest(new ApiMessage("Tema inválido."));
        }

        if (!AllowedAccentColors.Contains(accentColor))
        {
            return BadRequest(new ApiMessage("Cor principal inválida."));
        }

        var user = await _context.Users
            .FirstOrDefaultAsync(x => x.Id == userId && x.Role == "professional" && x.IsActive);

        if (user is null)
        {
            return NotFound(new ApiMessage("Usuário profissional não encontrado."));
        }

        var settings = await _context.UserSettings
            .FirstOrDefaultAsync(x => x.UserId == userId);

        if (settings is null)
        {
            settings = new UserSetting
            {
                UserId = userId,
                ThemeMode = theme,
                AccentColor = accentColor,
                LogoUrl = string.IsNullOrWhiteSpace(request.CompanyLogoUrl)
                    ? null
                    : request.CompanyLogoUrl.Trim(),
                LanguageCode = "pt-BR",
                ReminderMinutes = 60,
                EmailNotifications = false,
                WhatsAppNotifications = true,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            };

            _context.UserSettings.Add(settings);
        }
        else
        {
            settings.ThemeMode = theme;
            settings.AccentColor = accentColor;
            settings.LogoUrl = string.IsNullOrWhiteSpace(request.CompanyLogoUrl)
                ? null
                : request.CompanyLogoUrl.Trim();
            settings.UpdatedAt = DateTime.Now;
        }

        await _context.SaveChangesAsync();

        return Ok(new ApiMessage("Configurações visuais atualizadas com sucesso."));
    }
}
