using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Scheduler.Api.Data;
using Scheduler.Api.DTOs;
using Scheduler.Api.Entities;

namespace Scheduler.Api.Controllers;

[ApiController]
[Route("api/settings")]
public class SettingsController : ControllerBase
{
    private readonly AppDbContext _context;

    public SettingsController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<SettingsResponse>> Get([FromQuery] ulong userId)
    {
        if (userId == 0)
        {
            return BadRequest(new ApiMessage("Usuário inválido."));
        }

        var user = await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == userId && x.IsActive);

        if (user is null)
        {
            return NotFound(new ApiMessage("Usuário não encontrado."));
        }

        var settings = await _context.UserSettings
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.UserId == userId);

        if (settings is null)
        {
            settings = new UserSetting
            {
                UserId = userId,
                Theme = "light",
                AccentColor = "blue",
                CompanyLogoUrl = null,
                LanguageCode = "pt-BR",
                ReminderMinutes = 60,
                EmailNotifications = false,
                WhatsappNotifications = true,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            };

            _context.UserSettings.Add(settings);
            await _context.SaveChangesAsync();
        }

        return Ok(new SettingsResponse(
            settings.UserId,
            settings.Theme,
            settings.AccentColor,
            settings.CompanyLogoUrl
        ));
    }
}