using Scheduler.Api.Entities;
using Scheduler.Api.Services.Contracts;

namespace Scheduler.Api.Services;

public class BookingAutomationService : IBookingAutomationService
{
    private readonly IEmailService _emailService;
    private readonly IGoogleCalendarService _googleCalendarService;
    private readonly ILogger<BookingAutomationService> _logger;

    public BookingAutomationService(
        IEmailService emailService,
        IGoogleCalendarService googleCalendarService,
        ILogger<BookingAutomationService> logger)
    {
        _emailService = emailService;
        _googleCalendarService = googleCalendarService;
        _logger = logger;
    }

    public async Task<(bool ClientEmailSent, bool ProfessionalEmailSent, bool CalendarCreated)> ProcessAsync(
        User professional,
        UserSetting? userSetting,
        Client client,
        Service service,
        Appointment appointment)
    {
        var dateText = appointment.AppointmentDate.ToString("dd/MM/yyyy");
        var timeText = $"{appointment.StartTime:hh\\:mm} às {appointment.EndTime:hh\\:mm}";
        var businessName = string.IsNullOrWhiteSpace(professional.BusinessName)
            ? professional.FullName
            : professional.BusinessName;

        var clientEmailHtml =
            $"""
            <div style="font-family: Arial, sans-serif; color: #111827;">
              <h2>Agendamento confirmado</h2>
              <p>Olá, <strong>{client.FullName}</strong>!</p>
              <p>Seu agendamento foi realizado com sucesso.</p>
              <ul>
                <li><strong>Profissional:</strong> {businessName}</li>
                <li><strong>Serviço:</strong> {service.Name}</li>
                <li><strong>Data:</strong> {dateText}</li>
                <li><strong>Horário:</strong> {timeText}</li>
              </ul>
            </div>
            """;

        var professionalEmailHtml =
            $"""
            <div style="font-family: Arial, sans-serif; color: #111827;">
              <h2>Novo agendamento recebido</h2>
              <ul>
                <li><strong>Cliente:</strong> {client.FullName}</li>
                <li><strong>Telefone:</strong> {client.Phone}</li>
                <li><strong>E-mail:</strong> {client.Email}</li>
                <li><strong>Serviço:</strong> {service.Name}</li>
                <li><strong>Data:</strong> {dateText}</li>
                <li><strong>Horário:</strong> {timeText}</li>
              </ul>
            </div>
            """;

        var clientEmailSent = await _emailService.SendAsync(
            client.Email,
            "Agendamento confirmado",
            clientEmailHtml
        );

        var professionalEmailSent = await _emailService.SendAsync(
            professional.Email,
            "Novo agendamento recebido",
            professionalEmailHtml
        );

        var calendarCreated = await _googleCalendarService.CreateAppointmentEventAsync(
            professional,
            userSetting,
            client,
            service,
            appointment
        );

        return (clientEmailSent, professionalEmailSent, calendarCreated);
    }
}