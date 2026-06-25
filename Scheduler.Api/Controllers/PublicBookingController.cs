using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Scheduler.Api.Data;
using Scheduler.Api.DTOs;
using Scheduler.Api.Entities;
using Scheduler.Api.Services.Contracts;
using System.Globalization;
using System.Net.Mail;

namespace Scheduler.Api.Controllers;

[ApiController]
[Route("api/public/professionals")]
public class PublicBookingController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IBookingAutomationService _bookingAutomationService;

    public PublicBookingController(
        AppDbContext context,
        IBookingAutomationService bookingAutomationService)
    {
        _context = context;
        _bookingAutomationService = bookingAutomationService;
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

    [HttpPost("{slug}/appointments")]
    public async Task<ActionResult<PublicBookingSuccessResponse>> Create(
        string slug,
        [FromBody] PublicBookAppointmentRequest request)
    {
        var professional = await _context.Users
            .FirstOrDefaultAsync(x =>
                x.PublicSlug == slug &&
                x.Role == "professional" &&
                x.IsActive);

        if (professional is null)
        {
            return NotFound(new ApiMessage("Profissional não encontrado."));
        }

        var service = await _context.Services
            .FirstOrDefaultAsync(x =>
                x.Id == request.ServiceId &&
                x.UserId == professional.Id &&
                x.IsActive);

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

        if (request.AppointmentDate == default)
        {
            return BadRequest(new ApiMessage("Informe a data do agendamento."));
        }

        if (request.StartTime == default || request.EndTime == default)
        {
            return BadRequest(new ApiMessage("Informe o horário do agendamento."));
        }

        if (string.IsNullOrWhiteSpace(request.Email))
        {
            return BadRequest(new ApiMessage("Informe o e-mail do cliente."));
        }

        if (!IsValidEmail(request.Email))
        {
            return BadRequest(new ApiMessage("Informe um e-mail válido."));
        }

        var targetDate = request.AppointmentDate.Date;
        var startTime = request.StartTime;
        var endTime = request.EndTime;

        if (endTime <= startTime)
        {
            return BadRequest(new ApiMessage("O horário final deve ser maior que o horário inicial."));
        }

        var expectedEndTime = startTime.Add(TimeSpan.FromMinutes(service.DurationMinutes));

        if (endTime != expectedEndTime)
        {
            return BadRequest(new ApiMessage("O horário final não corresponde à duração do serviço."));
        }

        var windows = await GetAvailabilityWindowsAsync(professional.Id, targetDate);

        if (!windows.Any())
        {
            return Conflict(new ApiMessage("Não há disponibilidade cadastrada para essa data."));
        }

        var fitsWindow = windows.Any(x => startTime >= x.Start && endTime <= x.End);

        if (!fitsWindow)
        {
            return Conflict(new ApiMessage("Esse horário não está disponível dentro da agenda configurada."));
        }

        var appointments = await _context.Appointments
            .AsNoTracking()
            .Where(x =>
                x.UserId == professional.Id &&
                x.AppointmentDate == targetDate &&
                x.Status != "cancelled")
            .ToListAsync();

        var hasConflict = appointments.Any(x => startTime < x.EndTime && endTime > x.StartTime);

        if (hasConflict)
        {
            return Conflict(new ApiMessage("Esse horário acabou de ser reservado. Escolha outro."));
        }

        var normalizedPhone = NormalizePhone(request.Phone);

        var clients = await _context.Clients
            .Where(x => x.UserId == professional.Id)
            .ToListAsync();

        var client = clients.FirstOrDefault(x => NormalizePhone(x.Phone) == normalizedPhone);

        if (client is null)
        {
            client = new Client
            {
                UserId = professional.Id,
                FullName = request.FullName.Trim(),
                Email = request.Email.Trim(),
                Phone = request.Phone.Trim(),
                BirthDate = null,
                Notes = string.IsNullOrWhiteSpace(request.Notes)
                    ? "Criado automaticamente via agendamento público."
                    : request.Notes.Trim(),
                IsActive = true,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            };

            _context.Clients.Add(client);
            await _context.SaveChangesAsync();
        }
        else
        {
            var registeredClientEmail = await GetRegisteredClientEmailAsync(client.Id, professional.Id);

            client.FullName = request.FullName.Trim();
            client.Email = string.IsNullOrWhiteSpace(registeredClientEmail)
                ? request.Email.Trim()
                : registeredClientEmail.Trim();
            client.Phone = request.Phone.Trim();

            if (!string.IsNullOrWhiteSpace(request.Notes))
            {
                client.Notes = request.Notes.Trim();
            }

            client.IsActive = true;
            client.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();
        }

        var appointment = new Appointment
        {
            UserId = professional.Id,
            ClientId = client.Id,
            ServiceId = service.Id,
            AppointmentDate = targetDate,
            StartTime = startTime,
            EndTime = endTime,
            Status = "scheduled",
            PriceAtBooking = service.Price,
            Notes = string.IsNullOrWhiteSpace(request.Notes)
                ? "Agendado via página pública."
                : request.Notes.Trim(),
            CreatedAt = DateTime.Now,
            UpdatedAt = DateTime.Now
        };

        _context.Appointments.Add(appointment);
        await _context.SaveChangesAsync();

        var userSetting = await _context.UserSettings
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.UserId == professional.Id);

        var automationResult = await _bookingAutomationService.ProcessAsync(
            professional,
            userSetting,
            client,
            service,
            appointment
        );

        return Ok(new PublicBookingSuccessResponse(
            appointment.Id,
            client.FullName,
            service.Name,
            appointment.AppointmentDate.ToString("dd/MM/yyyy"),
            appointment.StartTime.ToString(@"hh\:mm"),
            appointment.EndTime.ToString(@"hh\:mm"),
            professional.FullName,
            professional.BusinessName,
            automationResult.ClientEmailSent,
            automationResult.ProfessionalEmailSent,
            automationResult.ClientWhatsAppSent,
            automationResult.ProfessionalWhatsAppSent,
            automationResult.ClientPushSent,
            automationResult.ProfessionalPushSent,
            automationResult.CalendarCreated,
            "Agendamento realizado com sucesso."
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
            client.IsActive = true;
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

    private static string NormalizePhone(string? phone)
    {
        return new string((phone ?? string.Empty).Where(char.IsDigit).ToArray());
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

    private sealed record AvailabilityWindow(TimeSpan Start, TimeSpan End);

    private static bool IsValidEmail(string? email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return false;
        }

        try
        {
            var parsed = new MailAddress(email.Trim());
            return parsed.Address.Equals(email.Trim(), StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return false;
        }
    }
}
