using Scheduler.Api.Data;
using Scheduler.Api.Entities;
using Scheduler.Api.Services.Contracts;
using Microsoft.EntityFrameworkCore;

namespace Scheduler.Api.Services;

public class InAppNotificationService : IInAppNotificationService
{
    private readonly AppDbContext _context;

    public InAppNotificationService(AppDbContext context)
    {
        _context = context;
    }

    public async Task CreateAppointmentNotificationsAsync(
        User professional,
        Client client,
        Service service,
        Appointment appointment,
        CancellationToken cancellationToken = default)
    {
        var dateText = appointment.AppointmentDate.ToString("dd/MM/yyyy");
        var timeText = $"{appointment.StartTime:hh\\:mm} às {appointment.EndTime:hh\\:mm}";
        var calendarUrl = CalendarUrlBuilder.Build(professional, client, service, appointment);

        _context.AppNotifications.Add(new AppNotification
        {
            UserId = professional.Id,
            AppointmentId = appointment.Id,
            Title = "Novo agendamento recebido",
            Message = $"{client.FullName} agendou {service.Name} em {dateText}, {timeText}.",
            ActionUrl = $"/appointments/{appointment.Id}",
            CalendarUrl = calendarUrl,
            CreatedAt = DateTime.Now
        });

        var clientUserId = await _context.Users
            .Where(x => x.Role == "client" &&
                        x.IsActive &&
                        x.ClientId == client.Id &&
                        x.ProfessionalUserId == professional.Id)
            .Select(x => (ulong?)x.Id)
            .FirstOrDefaultAsync(cancellationToken);

        if (clientUserId is not null)
        {
            _context.AppNotifications.Add(new AppNotification
            {
                UserId = clientUserId.Value,
                AppointmentId = appointment.Id,
                Title = "Agendamento aguardando aceite",
                Message = $"{service.Name} com {(string.IsNullOrWhiteSpace(professional.BusinessName) ? professional.FullName : professional.BusinessName)} em {dateText}, {timeText}.",
                ActionUrl = $"/client/appointments/{appointment.Id}",
                CalendarUrl = calendarUrl,
                CreatedAt = DateTime.Now
            });
        }

        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task NotifyAppointmentResponseAsync(
        User professional,
        Client client,
        Service service,
        Appointment appointment,
        string action,
        CancellationToken cancellationToken = default)
    {
        var accepted = action == "accepted";
        _context.AppNotifications.Add(new AppNotification
        {
            UserId = professional.Id,
            AppointmentId = appointment.Id,
            Title = accepted ? "Agendamento aceito" : "Agendamento recusado",
            Message = $"{client.FullName} {(accepted ? "aceitou" : "recusou")} o agendamento de {service.Name}.",
            ActionUrl = $"/appointments/{appointment.Id}",
            CalendarUrl = CalendarUrlBuilder.Build(professional, client, service, appointment),
            CreatedAt = DateTime.Now
        });

        await _context.SaveChangesAsync(cancellationToken);
    }
}

internal static class CalendarUrlBuilder
{
    public static string Build(User professional, Client client, Service service, Appointment appointment)
    {
        var start = new DateTime(
            appointment.AppointmentDate.Year, appointment.AppointmentDate.Month, appointment.AppointmentDate.Day,
            appointment.StartTime.Hours, appointment.StartTime.Minutes, 0, DateTimeKind.Unspecified);
        var end = new DateTime(
            appointment.AppointmentDate.Year, appointment.AppointmentDate.Month, appointment.AppointmentDate.Day,
            appointment.EndTime.Hours, appointment.EndTime.Minutes, 0, DateTimeKind.Unspecified);
        var timezone = string.IsNullOrWhiteSpace(professional.Timezone) ? "America/Sao_Paulo" : professional.Timezone;
        var details = $"Cliente: {client.FullName}\nTelefone: {client.Phone}\nServiço: {service.Name}";

        return "https://calendar.google.com/calendar/render?action=TEMPLATE" +
               $"&text={Uri.EscapeDataString(service.Name)}" +
               $"&dates={start:yyyyMMdd'T'HHmmss}/{end:yyyyMMdd'T'HHmmss}" +
               $"&ctz={Uri.EscapeDataString(timezone)}" +
               $"&details={Uri.EscapeDataString(details)}";
    }
}
