using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Scheduler.Api.Data;
using Scheduler.Api.DTOs;
using Scheduler.Api.Entities;
using Scheduler.Api.Services.Notifications;

namespace Scheduler.Api.Controllers;

[ApiController]
[Route("api/public/professionals")]
public class PublicBookingController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IAppointmentNotificationService _notificationService;

    public PublicBookingController(
        AppDbContext context,
        IAppointmentNotificationService notificationService)
    {
        _context = context;
        _notificationService = notificationService;
    }

    [HttpGet("{slug}")]
    public async Task<ActionResult<PublicBookingProfessionalResponse>> GetProfessional(string slug)
    {
        var professional = await GetProfessionalAsync(slug);

        if (professional is null)
        {
            return NotFound(new ApiMessage("Agenda não encontrada."));
        }

        var culture = CultureInfo.GetCultureInfo("pt-BR");

        var services = await _context.Services
            .AsNoTracking()
            .Where(x => x.UserId == professional.Id && x.IsActive)
            .OrderBy(x => x.Name)
            .ToListAsync();

        var response = new PublicBookingProfessionalResponse(
            professional.BusinessName ?? professional.FullName,
            professional.Specialty ?? "Agendamento online",
            professional.PublicSlug ?? slug,
            services.Select(x => new PublicBookingServiceResponse(
                x.Id,
                x.Name,
                x.Description,
                x.DurationMinutes,
                x.DurationMinutes < 60 ? $"{x.DurationMinutes} min" : $"{x.DurationMinutes / 60}h",
                x.Price,
                x.Price.ToString("C", culture)
            )).ToList()
        );

        return Ok(response);
    }

    [HttpGet("{slug}/available-slots")]
    public async Task<ActionResult<PublicBookingAvailableSlotsResponse>> GetAvailableSlots(
        string slug,
        [FromQuery] ulong serviceId,
        [FromQuery] string date)
    {
        var professional = await GetProfessionalAsync(slug);

        if (professional is null)
        {
            return NotFound(new ApiMessage("Agenda não encontrada."));
        }

        var service = await _context.Services
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == serviceId && x.UserId == professional.Id && x.IsActive);

        if (service is null)
        {
            return NotFound(new ApiMessage("Serviço não encontrado."));
        }

        if (!DateTime.TryParseExact(date, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var targetDate))
        {
            return BadRequest(new ApiMessage("Data inválida. Use o formato yyyy-MM-dd."));
        }

        var windows = await GetAvailabilityWindowsAsync(professional.Id, targetDate.Date);
        var slots = await GenerateAvailableSlotsAsync(professional.Id, targetDate.Date, service.DurationMinutes, windows);

        return Ok(new PublicBookingAvailableSlotsResponse(
            targetDate.ToString("yyyy-MM-dd"),
            service.Id,
            slots
        ));
    }

    [HttpPost("{slug}/book")]
    public async Task<ActionResult<PublicBookingCreatedResponse>> Book(
        string slug,
        [FromBody] PublicBookingRequest request)
    {
        var professional = await GetProfessionalAsync(slug);

        if (professional is null)
        {
            return NotFound(new ApiMessage("Agenda não encontrada."));
        }

        var service = await _context.Services
            .FirstOrDefaultAsync(x => x.Id == request.ServiceId && x.UserId == professional.Id && x.IsActive);

        if (service is null)
        {
            return NotFound(new ApiMessage("Serviço não encontrado."));
        }

        if (string.IsNullOrWhiteSpace(request.FullName))
        {
            return BadRequest(new ApiMessage("Informe o nome do cliente."));
        }

        if (string.IsNullOrWhiteSpace(request.Phone))
        {
            return BadRequest(new ApiMessage("Informe o telefone do cliente."));
        }

        if (!DateTime.TryParseExact(request.Date, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var targetDate))
        {
            return BadRequest(new ApiMessage("Data inválida. Use o formato yyyy-MM-dd."));
        }

        if (!TimeSpan.TryParseExact(request.Time, @"hh\:mm", CultureInfo.InvariantCulture, out var startTime))
        {
            return BadRequest(new ApiMessage("Horário inválido. Use o formato HH:mm."));
        }

        var endTime = startTime.Add(TimeSpan.FromMinutes(service.DurationMinutes));

        var windows = await GetAvailabilityWindowsAsync(professional.Id, targetDate.Date);
        var fitsWindow = windows.Any(x => startTime >= x.Start && endTime <= x.End);

        if (!fitsWindow)
        {
            return Conflict(new ApiMessage("Esse horário não está mais disponível."));
        }

        var appointments = await _context.Appointments
            .AsNoTracking()
            .Where(x =>
                x.UserId == professional.Id &&
                x.AppointmentDate == targetDate.Date &&
                x.Status != "cancelled")
            .ToListAsync();

        var hasConflict = appointments.Any(x => startTime < x.EndTime && endTime > x.StartTime);

        if (hasConflict)
        {
            return Conflict(new ApiMessage("Esse horário acabou de ser reservado. Escolha outro."));
        }

        var normalizedPhone = NormalizePhone(request.Phone);

        var clients = await _context.Clients
            .Where(x => x.UserId == professional.Id && x.IsActive)
            .ToListAsync();

        var client = clients.FirstOrDefault(x => NormalizePhone(x.Phone) == normalizedPhone);

        if (client is null)
        {
            client = new Client
            {
                UserId = professional.Id,
                FullName = request.FullName.Trim(),
                Email = null,
                Phone = request.Phone.Trim(),
                BirthDate = null,
                Notes = "Criado automaticamente via agendamento público.",
                IsActive = true,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            };

            _context.Clients.Add(client);
            await _context.SaveChangesAsync();
        }
        else
        {
            client.FullName = request.FullName.Trim();
            client.Phone = request.Phone.Trim();
            client.UpdatedAt = DateTime.Now;
            await _context.SaveChangesAsync();
        }

        var appointment = new Appointment
        {
            UserId = professional.Id,
            ClientId = client.Id,
            ServiceId = service.Id,
            AppointmentDate = targetDate.Date,
            StartTime = startTime,
            EndTime = endTime,
            Status = "scheduled",
            PriceAtBooking = service.Price,
            Notes = "Agendado via página pública.",
            CreatedAt = DateTime.Now,
            UpdatedAt = DateTime.Now
        };

        _context.Appointments.Add(appointment);
        await _context.SaveChangesAsync();

        await _notificationService.NotifyCreatedAsync(
            professional,
            client,
            service,
            appointment);

        return Ok(new PublicBookingCreatedResponse(
            appointment.Id,
            client.FullName,
            client.Phone,
            service.Name,
            targetDate.ToString("yyyy-MM-dd"),
            startTime.ToString(@"hh\:mm"),
            appointment.Status,
            "Agendamento realizado com sucesso."
        ));
    }

    private async Task<User?> GetProfessionalAsync(string slug)
    {
        return await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(x =>
                x.Role == "professional" &&
                x.IsActive &&
                x.PublicSlug == slug);
    }

    private async Task<List<AvailabilityWindow>> GetAvailabilityWindowsAsync(ulong userId, DateTime date)
    {
        var specificDates = await _context.AvailabilityDates
            .AsNoTracking()
            .Where(x => x.UserId == userId && x.AvailableDate == date)
            .OrderBy(x => x.StartTime)
            .ToListAsync();

        if (specificDates.Any())
        {
            return specificDates
                .Select(x => new AvailabilityWindow(x.StartTime, x.EndTime))
                .ToList();
        }

        var weekday = (int)date.DayOfWeek;

        var weekly = await _context.WeeklyAvailabilities
            .AsNoTracking()
            .Where(x => x.UserId == userId && x.IsActive && x.Weekday == weekday)
            .OrderBy(x => x.StartTime)
            .ToListAsync();

        return weekly
            .Select(x => new AvailabilityWindow(x.StartTime, x.EndTime))
            .ToList();
    }

    private async Task<List<string>> GenerateAvailableSlotsAsync(
        ulong userId,
        DateTime date,
        int durationMinutes,
        List<AvailabilityWindow> windows)
    {
        var duration = TimeSpan.FromMinutes(durationMinutes);

        var appointments = await _context.Appointments
            .AsNoTracking()
            .Where(x =>
                x.UserId == userId &&
                x.AppointmentDate == date &&
                x.Status != "cancelled")
            .ToListAsync();

        var slots = new List<string>();

        foreach (var window in windows)
        {
            var current = window.Start;

            while (current.Add(duration) <= window.End)
            {
                var end = current.Add(duration);

                var hasConflict = appointments.Any(x => current < x.EndTime && end > x.StartTime);

                if (!hasConflict)
                {
                    slots.Add(current.ToString(@"hh\:mm"));
                }

                current = current.Add(TimeSpan.FromMinutes(30));
            }
        }

        return slots
            .Distinct()
            .OrderBy(x => x)
            .ToList();
    }

    private static string NormalizePhone(string phone)
    {
        return new string(phone.Where(char.IsDigit).ToArray());
    }

    private sealed record AvailabilityWindow(TimeSpan Start, TimeSpan End);
}