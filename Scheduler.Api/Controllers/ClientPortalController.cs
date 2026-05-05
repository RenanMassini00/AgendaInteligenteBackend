using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Scheduler.Api.Data;
using Scheduler.Api.DTOs;
using Scheduler.Api.Entities;
using Scheduler.Api.Utils;
using System.Globalization;

namespace Scheduler.Api.Controllers;

[ApiController]
[Route("api/client")]
public class ClientPortalController : ControllerBase
{
    private readonly AppDbContext _context;

    public ClientPortalController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet("me")]
    public async Task<ActionResult<UserResponse>> Me([FromQuery] ulong userId)
    {
        var user = await _context.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Id == userId && x.Role == "client" && x.IsActive);
        if (user is null) return NotFound(new ApiMessage("Cliente não encontrado."));

        return Ok(new UserResponse(
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
        ));
    }

    [HttpGet("appointments")]
    public async Task<ActionResult<IEnumerable<AppointmentResponse>>> GetAppointments([FromQuery] ulong userId)
    {
        var user = await _context.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Id == userId && x.Role == "client" && x.IsActive);
        if (user is null || user.ClientId is null || user.ProfessionalUserId is null)
            return BadRequest(new ApiMessage("Sessão do cliente inválida."));

        var culture = CultureInfo.GetCultureInfo("pt-BR");
        var items = await _context.Appointments
            .AsNoTracking()
            .Include(x => x.Client)
            .Include(x => x.Service)
            .Where(x => x.UserId == user.ProfessionalUserId && x.ClientId == user.ClientId)
            .OrderByDescending(x => x.AppointmentDate)
            .ThenBy(x => x.StartTime)
            .ToListAsync();

        return Ok(items.Select(x => new AppointmentResponse(
            x.Id,
            x.ClientId,
            x.ServiceId,
            x.Client?.FullName ?? string.Empty,
            x.Service?.Name ?? string.Empty,
            x.AppointmentDate.ToString("yyyy-MM-dd"),
            x.StartTime.ToString(@"hh\:mm"),
            x.StartTime.ToString(@"hh\:mm"),
            x.EndTime.ToString(@"hh\:mm"),
            x.Status,
            x.PriceAtBooking,
            x.PriceAtBooking.ToString("C", culture),
            x.Notes
        )));
    }

    [HttpGet("available-slots")]
    public async Task<ActionResult<IEnumerable<AvailableSlotResponse>>> GetAvailableSlots([FromQuery] ulong userId, [FromQuery] ulong serviceId, [FromQuery] string date)
    {
        var user = await _context.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Id == userId && x.Role == "client" && x.IsActive);
        if (user is null || user.ProfessionalUserId is null)
            return BadRequest(new ApiMessage("Sessão do cliente inválida."));

        if (!DateTime.TryParse(date, out var parsedDate))
            return BadRequest(new ApiMessage("Data inválida. Use yyyy-MM-dd."));

        var service = await _context.Services.AsNoTracking().FirstOrDefaultAsync(x => x.Id == serviceId && x.UserId == user.ProfessionalUserId && x.IsActive);
        if (service is null) return BadRequest(new ApiMessage("Serviço inválido."));

        var availabilities = await _context.WeeklyAvailabilities.AsNoTracking().Where(x => x.UserId == user.ProfessionalUserId).ToListAsync();
        var appointments = await _context.Appointments.AsNoTracking().Where(x => x.UserId == user.ProfessionalUserId && x.AppointmentDate.Date == parsedDate.Date).ToListAsync();
        var blocked = await _context.BlockedPeriods.AsNoTracking().Where(x => x.UserId == user.ProfessionalUserId).ToListAsync();

        var slots = SlotCalculator.BuildAvailableSlots(parsedDate.Date, service.DurationMinutes, availabilities, appointments, blocked)
            .Select(x => new AvailableSlotResponse(x.Start.ToString(@"hh\:mm"), x.End.ToString(@"hh\:mm")))
            .ToList();

        return Ok(slots);
    }

    [HttpPost("appointments")]
    public async Task<ActionResult<AppointmentResponse>> CreateAppointment([FromQuery] ulong userId, ClientPortalAppointmentCreateRequest request)
    {
        var user = await _context.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Id == userId && x.Role == "client" && x.IsActive);
        if (user is null || user.ClientId is null || user.ProfessionalUserId is null)
            return BadRequest(new ApiMessage("Sessão do cliente inválida."));

        if (request.ProfessionalUserId != user.ProfessionalUserId)
            return BadRequest(new ApiMessage("Profissional inválido para este cliente."));

        if (!DateTime.TryParse(request.Date, out var date))
            return BadRequest(new ApiMessage("Data inválida. Use yyyy-MM-dd."));

        if (!TimeSpan.TryParse(request.Time, out var startTime))
            return BadRequest(new ApiMessage("Horário inválido. Use HH:mm."));

        var service = await _context.Services.FirstOrDefaultAsync(x => x.Id == request.ServiceId && x.UserId == user.ProfessionalUserId && x.IsActive);
        if (service is null) return BadRequest(new ApiMessage("Serviço inválido."));

        var endTime = startTime.Add(TimeSpan.FromMinutes(service.DurationMinutes));

        var hasConflict = await _context.Appointments.AnyAsync(x =>
            x.UserId == user.ProfessionalUserId &&
            x.AppointmentDate.Date == date.Date &&
            x.Status != "cancelled" &&
            startTime < x.EndTime &&
            endTime > x.StartTime
        );

        if (hasConflict)
            return Conflict(new ApiMessage("Já existe um agendamento nesse intervalo de horário."));

        var appointment = new Appointment
        {
            UserId = user.ProfessionalUserId.Value,
            ClientId = user.ClientId.Value,
            ServiceId = request.ServiceId,
            AppointmentDate = date.Date,
            StartTime = startTime,
            EndTime = endTime,
            Status = "scheduled",
            PriceAtBooking = service.Price,
            Notes = request.Notes,
            CreatedAt = DateTime.Now,
            UpdatedAt = DateTime.Now
        };

        _context.Appointments.Add(appointment);
        await _context.SaveChangesAsync();

        _context.AppointmentStatusHistory.Add(new AppointmentStatusHistory
        {
            AppointmentId = appointment.Id,
            PreviousStatus = null,
            NewStatus = appointment.Status,
            ChangedByUserId = user.Id,
            Note = "Agendamento criado pelo cliente",
            CreatedAt = DateTime.Now
        });
        await _context.SaveChangesAsync();

        var created = await _context.Appointments
            .AsNoTracking()
            .Include(x => x.Client)
            .Include(x => x.Service)
            .FirstAsync(x => x.Id == appointment.Id);

        var culture = CultureInfo.GetCultureInfo("pt-BR");
        return Ok(new AppointmentResponse(
            created.Id,
            created.ClientId,
            created.ServiceId,
            created.Client?.FullName ?? string.Empty,
            created.Service?.Name ?? string.Empty,
            created.AppointmentDate.ToString("yyyy-MM-dd"),
            created.StartTime.ToString(@"hh\:mm"),
            created.StartTime.ToString(@"hh\:mm"),
            created.EndTime.ToString(@"hh\:mm"),
            created.Status,
            created.PriceAtBooking,
            created.PriceAtBooking.ToString("C", culture),
            created.Notes
        ));
    }
}
