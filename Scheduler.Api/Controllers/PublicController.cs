using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Scheduler.Api.Data;
using Scheduler.Api.DTOs;
using Scheduler.Api.Utils;
using System.Globalization;

namespace Scheduler.Api.Controllers;

[ApiController]
[Route("api/public")]
public class PublicController : ControllerBase
{
    private readonly AppDbContext _context;

    public PublicController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet("professionals")]
    public async Task<ActionResult<IEnumerable<PublicProfessionalResponse>>> GetProfessionals()
    {
        var items = await _context.Users
            .AsNoTracking()
            .Where(x => x.Role == "professional" && x.IsActive)
            .OrderBy(x => x.BusinessName ?? x.FullName)
            .Select(x => new PublicProfessionalResponse(x.Id, x.FullName, x.BusinessName, x.Email, x.Specialty, x.PublicSlug))
            .ToListAsync();

        return Ok(items);
    }

    [HttpGet("professionals/{professionalUserId}/services")]
    public async Task<ActionResult<IEnumerable<ServiceResponse>>> GetServices(ulong professionalUserId)
    {
        var culture = CultureInfo.GetCultureInfo("pt-BR");
        var services = await _context.Services
            .AsNoTracking()
            .Where(x => x.UserId == professionalUserId && x.IsActive)
            .OrderBy(x => x.Name)
            .ToListAsync();

        return Ok(services.Select(x => new ServiceResponse(
            x.Id,
            x.Name,
            x.Description,
            x.DurationMinutes,
            x.DurationMinutes < 60 ? $"{x.DurationMinutes} min" : $"{x.DurationMinutes / 60}h",
            x.Price,
            x.Price.ToString("C", culture),
            x.ColorHex
        )));
    }

    [HttpGet("professionals/{professionalUserId}/available-slots")]
    public async Task<ActionResult<IEnumerable<AvailableSlotResponse>>> GetAvailableSlots(
        ulong professionalUserId,
        [FromQuery] ulong serviceId,
        [FromQuery] string date)
    {
        if (!DateTime.TryParse(date, out var parsedDate))
            return BadRequest(new ApiMessage("Data inválida. Use yyyy-MM-dd."));

        var service = await _context.Services.AsNoTracking().FirstOrDefaultAsync(x => x.Id == serviceId && x.UserId == professionalUserId && x.IsActive);
        if (service is null) return BadRequest(new ApiMessage("Serviço inválido."));

        var availabilities = await _context.WeeklyAvailabilities.AsNoTracking().Where(x => x.UserId == professionalUserId).ToListAsync();
        var appointments = await _context.Appointments.AsNoTracking().Where(x => x.UserId == professionalUserId && x.AppointmentDate.Date == parsedDate.Date).ToListAsync();
        var blocked = await _context.BlockedPeriods.AsNoTracking().Where(x => x.UserId == professionalUserId).ToListAsync();

        var slots = SlotCalculator.BuildAvailableSlots(parsedDate.Date, service.DurationMinutes, availabilities, appointments, blocked)
            .Select(x => new AvailableSlotResponse(x.Start.ToString(@"hh\:mm"), x.End.ToString(@"hh\:mm")))
            .ToList();

        return Ok(slots);
    }
}
