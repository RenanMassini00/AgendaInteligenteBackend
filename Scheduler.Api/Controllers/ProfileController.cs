using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Scheduler.Api.Data;
using Scheduler.Api.DTOs;

namespace Scheduler.Api.Controllers;

[ApiController]
[Route("api/profile")]
public class ProfileController : ControllerBase
{
    private readonly AppDbContext _context;

    public ProfileController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<UserResponse>> Get([FromQuery] ulong userId = 1)
    {
        var user = await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == userId && x.IsActive);

        if (user is null)
        {
            return NotFound(new ApiMessage("Usuário não encontrado."));
        }

        var userSetting = await _context.UserSettings
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.UserId == user.Id);

        return Ok(new UserResponse(
            Id: user.Id,
            FullName: user.FullName,
            BusinessName: user.BusinessName,
            Email: user.Email,
            Phone: user.Phone,
            Specialty: user.Specialty,
            Timezone: user.Timezone,
            Role: NormalizeRole(user.Role),
            PublicSlug: user.PublicSlug,
            ProfessionalUserId: user.ProfessionalUserId,
            ClientId: user.ClientId,
            HasAppointmentsModule: user.HasAppointmentsModule,
            HasCatalogModule: user.HasCatalogModule,
            ThemeMode: userSetting?.ThemeMode ?? "light",
            AccentColor: userSetting?.AccentColor ?? "azul",
            LogoUrl: userSetting?.LogoUrl
        ));
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
}