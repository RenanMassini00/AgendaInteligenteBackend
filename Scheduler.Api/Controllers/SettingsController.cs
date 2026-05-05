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
    public async Task<ActionResult<SettingsResponse>> Get([FromQuery] ulong userId = 1)
    {
        var settings = await _context.UserSettings.AsNoTracking().FirstOrDefaultAsync(x => x.UserId == userId);
        if (settings is null)
        {
            settings = new UserSetting
            {
                UserId = userId,
                Theme = "light",
                LanguageCode = "pt-BR",
                ReminderMinutes = 60,
                EmailNotifications = false,
                WhatsappNotifications = false,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            };

            _context.UserSettings.Add(settings);
            await _context.SaveChangesAsync();
        }

        return Ok(ToResponse(settings));
    }

    [HttpPut]
    public async Task<ActionResult<SettingsResponse>> Update([FromQuery] ulong userId, SettingsUpdateRequest request)
    {
        var settings = await _context.UserSettings.FirstOrDefaultAsync(x => x.UserId == userId);
        if (settings is null) return NotFound(new ApiMessage("Configurações não encontradas."));

        settings.Theme = request.Theme;
        settings.LanguageCode = request.LanguageCode;
        settings.ReminderMinutes = request.ReminderMinutes;
        settings.EmailNotifications = request.EmailNotifications;
        settings.WhatsappNotifications = request.WhatsappNotifications;
        settings.UpdatedAt = DateTime.Now;

        await _context.SaveChangesAsync();
        return Ok(ToResponse(settings));
    }

    private static SettingsResponse ToResponse(UserSetting settings) => new(
        settings.Id,
        settings.UserId,
        settings.Theme,
        settings.LanguageCode,
        settings.ReminderMinutes,
        settings.EmailNotifications,
        settings.WhatsappNotifications
    );
}
