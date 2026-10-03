using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Scheduler.Api.Data;
using Scheduler.Api.DTOs;
using Scheduler.Api.Entities;
using Scheduler.Api.Services.Contracts;
using Scheduler.Api.Services.Notifications;
using Scheduler.Api.Services;
using System.Globalization;

namespace Scheduler.Api.Controllers;

[ApiController]
[Route("api/appointments")]
[Authorize(Roles = "professional,employee")]
public class AppointmentsController : ControllerBase
{
    private static readonly string[] ValidStatuses = ["scheduled", "confirmed", "completed", "cancelled", "no_show"];
    private readonly AppDbContext _context;
    private readonly IAppointmentNotificationService _notificationService;
    private readonly IBookingAutomationService _bookingAutomationService;
    private readonly AuthenticatedUserScope _userScope;

    public AppointmentsController(
        AppDbContext context,
        IAppointmentNotificationService notificationService,
        IBookingAutomationService bookingAutomationService,
        AuthenticatedUserScope userScope)
    {
        _context = context;
        _notificationService = notificationService;
        _bookingAutomationService = bookingAutomationService;
        _userScope = userScope;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<AppointmentResponse>>> GetAll([FromQuery] ulong userId = 0, [FromQuery] string? date = null)
    {
        var user = await _userScope.GetCurrentUserAsync(User);
        if (user is null)
            return Unauthorized(new ApiMessage("Usuário não encontrado ou inativo."));

        if (await _userScope.GetBusinessOwnerAsync(user) is null)
            return Forbid();

        var professionalId = await _userScope.ResolveProfessionalIdAsync(user, userId);
        if (professionalId is null)
            return Forbid();

        var query = _context.Appointments
            .AsNoTracking()
            .Include(x => x.Client)
            .Include(x => x.Service)
            .Where(x => x.UserId == professionalId.Value);

        if (!string.IsNullOrWhiteSpace(date) && DateTime.TryParse(date, out var parsedDate))
        {
            query = query.Where(x => x.AppointmentDate.Date == parsedDate.Date);
        }

        var appointments = await query
            .OrderBy(x => x.AppointmentDate)
            .ThenBy(x => x.StartTime)
            .ToListAsync();

        return Ok(appointments.Select(ToResponse));
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<AppointmentResponse>> GetById(ulong id)
    {
        var user = await _userScope.GetCurrentUserAsync(User);
        if (user is null)
            return Unauthorized(new ApiMessage("Usuário não encontrado ou inativo."));

        if (await _userScope.GetBusinessOwnerAsync(user) is null)
            return Forbid();

        var accessibleProfessionalIds = await _userScope.GetAccessibleProfessionalIdsAsync(user);
        var appointment = await _context.Appointments
            .AsNoTracking()
            .Include(x => x.Client)
            .Include(x => x.Service)
            .FirstOrDefaultAsync(x => x.Id == id && accessibleProfessionalIds.Contains(x.UserId));

        if (appointment is null) return NotFound(new ApiMessage("Agendamento não encontrado."));
        return Ok(ToResponse(appointment));
    }

    [HttpPost]
    public async Task<ActionResult<AppointmentResponse>> Create(AppointmentCreateRequest request)
    {
        var user = await _userScope.GetCurrentUserAsync(User);
        if (user is null)
            return Unauthorized(new ApiMessage("Usuário não encontrado ou inativo."));

        var owner = await _userScope.GetBusinessOwnerAsync(user);
        var resolvedProfessionalId = await _userScope.ResolveProfessionalIdAsync(user, request.UserId);
        if (owner is null || resolvedProfessionalId is null)
            return Forbid();

        var userId = resolvedProfessionalId.Value;

        if (!ValidStatuses.Contains(request.Status))
            return BadRequest(new ApiMessage("Status inválido."));

        if (!DateTime.TryParse(request.Date, out var date))
            return BadRequest(new ApiMessage("Data inválida. Use yyyy-MM-dd."));

        if (!TimeSpan.TryParse(request.Time, out var startTime))
            return BadRequest(new ApiMessage("Horário inválido. Use HH:mm."));

        var client = await _context.Clients.FirstOrDefaultAsync(x => x.Id == request.ClientId && x.UserId == owner.Id && x.IsActive);
        if (client is null) return BadRequest(new ApiMessage("Cliente inválido."));

        var service = await _context.Services.FirstOrDefaultAsync(x => x.Id == request.ServiceId && x.UserId == owner.Id && x.IsActive);
        if (service is null) return BadRequest(new ApiMessage("Serviço inválido."));

        var professional = await _context.Users.FirstOrDefaultAsync(x => x.Id == userId && x.IsActive);
        if (professional is null) return BadRequest(new ApiMessage("Profissional inválido."));

        var registeredClientEmail = await GetRegisteredClientEmailAsync(client.Id, owner.Id);
        if (!string.IsNullOrWhiteSpace(registeredClientEmail))
        {
            client.Email = registeredClientEmail.Trim();
            client.UpdatedAt = DateTime.Now;
        }

        var endTime = startTime.Add(TimeSpan.FromMinutes(service.DurationMinutes));

        var hasConflict = await _context.Appointments.AnyAsync(x =>
            x.UserId == userId &&
            x.AppointmentDate.Date == date.Date &&
            x.Status != "cancelled" &&
            startTime < x.EndTime &&
            endTime > x.StartTime
        );

        if (hasConflict)
            return Conflict(new ApiMessage("Já existe um agendamento nesse intervalo de horário."));

        var appointment = new Appointment
        {
            UserId = userId,
            ClientId = request.ClientId,
            ServiceId = request.ServiceId,
            AppointmentDate = date.Date,
            StartTime = startTime,
            EndTime = endTime,
            Status = request.Status,
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
            Note = "Agendamento criado",
            CreatedAt = DateTime.Now
        });
        await _context.SaveChangesAsync();

        var userSetting = await _context.UserSettings
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.UserId == professional.Id);

        await _bookingAutomationService.ProcessAsync(
            professional,
            userSetting,
            client,
            service,
            appointment);

        var created = await _context.Appointments
            .AsNoTracking()
            .Include(x => x.Client)
            .Include(x => x.Service)
            .FirstAsync(x => x.Id == appointment.Id);

        return CreatedAtAction(nameof(GetById), new { id = appointment.Id }, ToResponse(created));
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<AppointmentResponse>> Update(ulong id, AppointmentUpdateRequest request)
    {
        var user = await _userScope.GetCurrentUserAsync(User);
        if (user is null)
            return Unauthorized(new ApiMessage("Usuário não encontrado ou inativo."));

        var owner = await _userScope.GetBusinessOwnerAsync(user);
        if (owner is null)
            return Forbid();

        var accessibleProfessionalIds = await _userScope.GetAccessibleProfessionalIdsAsync(user);
        var appointment = await _context.Appointments.FirstOrDefaultAsync(x =>
            x.Id == id && accessibleProfessionalIds.Contains(x.UserId));
        if (appointment is null)
            return NotFound(new ApiMessage("Agendamento não encontrado."));

        var requestedProfessionalId = request.UserId == 0 ? appointment.UserId : request.UserId;
        var resolvedProfessionalId = await _userScope.ResolveProfessionalIdAsync(user, requestedProfessionalId);
        if (resolvedProfessionalId is null)
            return Forbid();

        var userId = resolvedProfessionalId.Value;

        if (!ValidStatuses.Contains(request.Status))
            return BadRequest(new ApiMessage("Status inválido."));

        if (!DateTime.TryParse(request.Date, out var date))
            return BadRequest(new ApiMessage("Data inválida. Use yyyy-MM-dd."));

        if (!TimeSpan.TryParse(request.Time, out var startTime))
            return BadRequest(new ApiMessage("Horário inválido. Use HH:mm."));

        var client = await _context.Clients.FirstOrDefaultAsync(x => x.Id == request.ClientId && x.UserId == owner.Id && x.IsActive);
        if (client is null) return BadRequest(new ApiMessage("Cliente inválido."));

        var service = await _context.Services.FirstOrDefaultAsync(x => x.Id == request.ServiceId && x.UserId == owner.Id && x.IsActive);
        if (service is null) return BadRequest(new ApiMessage("Serviço inválido."));

        var professional = await _context.Users.FirstOrDefaultAsync(x => x.Id == userId && x.IsActive);
        if (professional is null) return BadRequest(new ApiMessage("Profissional inválido."));

        var endTime = startTime.Add(TimeSpan.FromMinutes(service.DurationMinutes));

        var hasConflict = await _context.Appointments.AnyAsync(x =>
            x.Id != id &&
            x.UserId == userId &&
            x.AppointmentDate.Date == date.Date &&
            x.Status != "cancelled" &&
            startTime < x.EndTime &&
            endTime > x.StartTime
        );

        if (hasConflict)
            return Conflict(new ApiMessage("Já existe um agendamento nesse intervalo de horário."));

        if (request.Status == "confirmed" &&
            await _context.AppointmentPayments.AnyAsync(x =>
                x.AppointmentId == appointment.Id &&
                x.Status != "approved"))
        {
            return Conflict(new ApiMessage("O sinal precisa ser aprovado pelo Mercado Pago antes de confirmar este agendamento."));
        }

        var previousStatus = appointment.Status;

        appointment.UserId = userId;
        appointment.ClientId = request.ClientId;
        appointment.ServiceId = request.ServiceId;
        appointment.AppointmentDate = date.Date;
        appointment.StartTime = startTime;
        appointment.EndTime = endTime;
        appointment.Status = request.Status;
        appointment.PriceAtBooking = service.Price;
        appointment.Notes = request.Notes;
        appointment.UpdatedAt = DateTime.Now;

        if (request.Status == "cancelled")
        {
            var pendingPayment = await _context.AppointmentPayments
                .FirstOrDefaultAsync(x => x.AppointmentId == appointment.Id && x.Status == "pending");
            if (pendingPayment is not null)
            {
                pendingPayment.Status = "cancelled";
                pendingPayment.UpdatedAt = DateTime.UtcNow;
            }
        }

        await _context.SaveChangesAsync();

        _context.AppointmentStatusHistory.Add(new AppointmentStatusHistory
        {
            AppointmentId = appointment.Id,
            PreviousStatus = previousStatus,
            NewStatus = appointment.Status,
            ChangedByUserId = user.Id,
            Note = "Agendamento alterado",
            CreatedAt = DateTime.Now
        });
        await _context.SaveChangesAsync();

        await _notificationService.NotifyUpdatedAsync(
            professional,
            client,
            service,
            appointment);

        var updated = await _context.Appointments
            .AsNoTracking()
            .Include(x => x.Client)
            .Include(x => x.Service)
            .FirstAsync(x => x.Id == appointment.Id);

        return Ok(ToResponse(updated));
    }

    [HttpPatch("{id}/status")]
    public async Task<ActionResult<ApiMessage>> UpdateStatus(
        ulong id,
        [FromBody] AppointmentStatusUpdateRequest request)
    {
        var user = await _userScope.GetCurrentUserAsync(User);
        if (user is null)
            return Unauthorized(new ApiMessage("Usuário não encontrado ou inativo."));

        if (await _userScope.GetBusinessOwnerAsync(user) is null)
            return Forbid();

        var accessibleProfessionalIds = await _userScope.GetAccessibleProfessionalIdsAsync(user);
        var appointment = await _context.Appointments.FirstOrDefaultAsync(x =>
            x.Id == id && accessibleProfessionalIds.Contains(x.UserId));

        if (appointment is null)
        {
            return NotFound(new ApiMessage("Agendamento não encontrado."));
        }

        var normalized = (request.Status ?? string.Empty).Trim().ToLower();

        var allowed = new[] { "scheduled", "confirmed", "completed", "cancelled" };
        if (!allowed.Contains(normalized))
        {
            return BadRequest(new ApiMessage("Status inválido."));
        }

        if (normalized == "confirmed" &&
            await _context.AppointmentPayments.AnyAsync(x =>
                x.AppointmentId == appointment.Id &&
                x.Status != "approved"))
        {
            return Conflict(new ApiMessage("O sinal precisa ser aprovado pelo Mercado Pago antes de confirmar este agendamento."));
        }

        var previousStatus = appointment.Status;
        appointment.Status = normalized;
        appointment.UpdatedAt = DateTime.Now;

        if (normalized == "cancelled")
        {
            var pendingPayment = await _context.AppointmentPayments
                .FirstOrDefaultAsync(x => x.AppointmentId == appointment.Id && x.Status == "pending");
            if (pendingPayment is not null)
            {
                pendingPayment.Status = "cancelled";
                pendingPayment.UpdatedAt = DateTime.UtcNow;
            }
        }

        _context.AppointmentStatusHistory.Add(new AppointmentStatusHistory
        {
            AppointmentId = appointment.Id,
            PreviousStatus = previousStatus,
            NewStatus = normalized,
            ChangedByUserId = user.Id,
            Note = $"Status alterado para {normalized}",
            CreatedAt = DateTime.Now
        });

        await _context.SaveChangesAsync();

        var client = await _context.Clients.FirstOrDefaultAsync(x => x.Id == appointment.ClientId);
        var service = await _context.Services.FirstOrDefaultAsync(x => x.Id == appointment.ServiceId);
        var professional = await _context.Users.FirstOrDefaultAsync(x => x.Id == appointment.UserId);

        if (client is not null && service is not null && professional is not null)
        {
            if (normalized == "completed")
            {
                await _notificationService.NotifyCompletedAsync(professional, client, service, appointment);
            }
            else if (normalized == "cancelled")
            {
                await _notificationService.NotifyCancelledAsync(professional, client, service, appointment);
            }
        }

        return Ok(new ApiMessage("Status atualizado com sucesso."));
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult<ApiMessage>> Cancel(ulong id)
    {
        var user = await _userScope.GetCurrentUserAsync(User);
        if (user is null)
            return Unauthorized(new ApiMessage("Usuário não encontrado ou inativo."));

        if (await _userScope.GetBusinessOwnerAsync(user) is null)
            return Forbid();

        var accessibleProfessionalIds = await _userScope.GetAccessibleProfessionalIdsAsync(user);
        var appointment = await _context.Appointments.FirstOrDefaultAsync(x =>
            x.Id == id && accessibleProfessionalIds.Contains(x.UserId));
        if (appointment is null) return NotFound(new ApiMessage("Agendamento não encontrado."));

        var previousStatus = appointment.Status;
        appointment.Status = "cancelled";
        appointment.CancelledReason = "Cancelado pelo sistema";
        appointment.UpdatedAt = DateTime.Now;

        _context.AppointmentStatusHistory.Add(new AppointmentStatusHistory
        {
            AppointmentId = appointment.Id,
            PreviousStatus = previousStatus,
            NewStatus = "cancelled",
            ChangedByUserId = user.Id,
            Note = appointment.CancelledReason,
            CreatedAt = DateTime.Now
        });

        await _context.SaveChangesAsync();

        var client = await _context.Clients.FirstOrDefaultAsync(x => x.Id == appointment.ClientId);
        var service = await _context.Services.FirstOrDefaultAsync(x => x.Id == appointment.ServiceId);
        var professional = await _context.Users.FirstOrDefaultAsync(x => x.Id == appointment.UserId);

        if (client is not null && service is not null && professional is not null)
        {
            await _notificationService.NotifyCancelledAsync(professional, client, service, appointment);
        }

        return Ok(new ApiMessage("Agendamento cancelado com sucesso."));
    }

    private async Task<string?> GetRegisteredClientEmailAsync(ulong clientId, ulong professionalUserId)
    {
        return await _context.Users
            .AsNoTracking()
            .Where(x =>
                x.Role == "client" &&
                x.IsActive &&
                x.ClientId == clientId &&
                x.ProfessionalUserId == professionalUserId)
            .Select(x => x.Email)
            .FirstOrDefaultAsync();
    }

    private static AppointmentResponse ToResponse(Appointment appointment) => new(
        appointment.Id,
        appointment.ClientId,
        appointment.ServiceId,
        appointment.Client?.FullName ?? string.Empty,
        appointment.Service?.Name ?? string.Empty,
        appointment.AppointmentDate.ToString("yyyy-MM-dd"),
        appointment.StartTime.ToString(@"hh\:mm"),
        appointment.StartTime.ToString(@"hh\:mm"),
        appointment.EndTime.ToString(@"hh\:mm"),
        appointment.Status,
        appointment.PriceAtBooking,
        appointment.PriceAtBooking.ToString("C", CultureInfo.GetCultureInfo("pt-BR")),
        appointment.Notes
    );
}
