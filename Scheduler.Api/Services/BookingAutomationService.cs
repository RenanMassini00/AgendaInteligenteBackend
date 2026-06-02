using Scheduler.Api.Entities;
using Scheduler.Api.Services.Contracts;
using System.Text;

namespace Scheduler.Api.Services;

public class BookingAutomationService : IBookingAutomationService
{
    private readonly IEmailService _emailService;
    private readonly IGoogleCalendarService _googleCalendarService;
    private readonly IWhatsAppService _whatsAppService;
    private readonly ILogger<BookingAutomationService> _logger;

    public BookingAutomationService(
        IEmailService emailService,
        IGoogleCalendarService googleCalendarService,
        IWhatsAppService whatsAppService,
        ILogger<BookingAutomationService> logger)
    {
        _emailService = emailService;
        _googleCalendarService = googleCalendarService;
        _whatsAppService = whatsAppService;
        _logger = logger;
    }

    public async Task<(
        bool ClientEmailSent,
        bool ProfessionalEmailSent,
        bool ClientWhatsAppSent,
        bool ProfessionalWhatsAppSent,
        bool CalendarCreated
    )> ProcessAsync(
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

        var googleCalendarUrl = BuildGoogleCalendarUrl(
            professional,
            client,
            service,
            appointment
        );

        _logger.LogInformation(
            "Iniciando automação do agendamento {AppointmentId}. ClienteEmail: {ClientEmail}. ProfessionalEmail: {ProfessionalEmail}",
            appointment.Id,
            client.Email,
            professional.Email
        );

        var clientEmailHtml =
            $"""
            <div style="font-family: Arial, sans-serif; color: #111827; line-height: 1.6;">
              <h2 style="margin-bottom: 16px;">Agendamento confirmado</h2>

              <p>Olá, <strong>{client.FullName}</strong>!</p>
              <p>Seu agendamento foi realizado com sucesso.</p>

              <ul>
                <li><strong>Profissional:</strong> {businessName}</li>
                <li><strong>Serviço:</strong> {service.Name}</li>
                <li><strong>Data:</strong> {dateText}</li>
                <li><strong>Horário:</strong> {timeText}</li>
              </ul>

              <p>Nos vemos em breve.</p>

              <div style="margin-top: 24px;">
                <a href="{googleCalendarUrl}"
                   target="_blank"
                   style="
                     display: inline-block;
                     background: #0f766e;
                     color: #ffffff;
                     text-decoration: none;
                     padding: 12px 20px;
                     border-radius: 10px;
                     font-weight: 700;
                   ">
                  Adicionar ao meu calendário
                </a>
              </div>
            </div>
            """;

        var professionalEmailHtml =
            $"""
            <div style="font-family: Arial, sans-serif; color: #111827; line-height: 1.6;">
              <h2 style="margin-bottom: 16px;">Novo agendamento recebido</h2>

              <p>Você recebeu um novo agendamento.</p>

              <ul>
                <li><strong>Cliente:</strong> {client.FullName}</li>
                <li><strong>Telefone:</strong> {client.Phone}</li>
                <li><strong>E-mail:</strong> {client.Email ?? "Não informado"}</li>
                <li><strong>Serviço:</strong> {service.Name}</li>
                <li><strong>Data:</strong> {dateText}</li>
                <li><strong>Horário:</strong> {timeText}</li>
                <li><strong>Observações:</strong> {appointment.Notes ?? "Sem observações"}</li>
              </ul>

              <div style="margin-top: 24px;">
                <a href="{googleCalendarUrl}"
                   target="_blank"
                   style="
                     display: inline-block;
                     background: #1d4ed8;
                     color: #ffffff;
                     text-decoration: none;
                     padding: 12px 20px;
                     border-radius: 10px;
                     font-weight: 700;
                   ">
                  Adicionar ao Google Agenda
                </a>
              </div>
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

        _logger.LogInformation(
            "Resultado dos e-mails do agendamento {AppointmentId}. Cliente: {ClientEmailSent}. Profissional: {ProfessionalEmailSent}",
            appointment.Id,
            clientEmailSent,
            professionalEmailSent
        );

        var clientWhatsAppSent = await _whatsAppService.SendTextAsync(
            client.Phone,
            $"""
            Olá, {client.FullName}! Seu agendamento foi confirmado com sucesso.

            Profissional: {businessName}
            Serviço: {service.Name}
            Data: {dateText}
            Horário: {timeText}
            """
        );

        var professionalWhatsAppSent = await _whatsAppService.SendTextAsync(
            professional.Phone,
            $"""
            Novo agendamento recebido.

            Cliente: {client.FullName}
            Telefone: {client.Phone}
            E-mail: {client.Email ?? "Não informado"}
            Serviço: {service.Name}
            Data: {dateText}
            Horário: {timeText}
            """
        );

        var calendarCreated = await _googleCalendarService.CreateAppointmentEventAsync(
            professional,
            userSetting,
            client,
            service,
            appointment
        );

        return (
            clientEmailSent,
            professionalEmailSent,
            clientWhatsAppSent,
            professionalWhatsAppSent,
            calendarCreated
        );
    }

    private static string BuildGoogleCalendarUrl(
        User professional,
        Client client,
        Service service,
        Appointment appointment)
    {
        var timezone = string.IsNullOrWhiteSpace(professional.Timezone)
            ? "America/Sao_Paulo"
            : professional.Timezone;

        var startDateTime = new DateTime(
            appointment.AppointmentDate.Year,
            appointment.AppointmentDate.Month,
            appointment.AppointmentDate.Day,
            appointment.StartTime.Hours,
            appointment.StartTime.Minutes,
            0
        );

        var endDateTime = new DateTime(
            appointment.AppointmentDate.Year,
            appointment.AppointmentDate.Month,
            appointment.AppointmentDate.Day,
            appointment.EndTime.Hours,
            appointment.EndTime.Minutes,
            0
        );

        var title = $"{service.Name} - {client.FullName}";

        var detailsBuilder = new StringBuilder();
        detailsBuilder.AppendLine("Agendamento confirmado");
        detailsBuilder.AppendLine();
        detailsBuilder.AppendLine($"Profissional: {professional.BusinessName ?? professional.FullName}");
        detailsBuilder.AppendLine($"Cliente: {client.FullName}");
        detailsBuilder.AppendLine($"Serviço: {service.Name}");
        detailsBuilder.AppendLine($"Telefone: {client.Phone}");

        if (!string.IsNullOrWhiteSpace(client.Email))
        {
            detailsBuilder.AppendLine($"E-mail: {client.Email}");
        }

        if (!string.IsNullOrWhiteSpace(appointment.Notes))
        {
            detailsBuilder.AppendLine($"Observações: {appointment.Notes}");
        }

        var details = detailsBuilder.ToString();

        var startText = startDateTime.ToString("yyyyMMdd'T'HHmmss");
        var endText = endDateTime.ToString("yyyyMMdd'T'HHmmss");

        return "https://calendar.google.com/calendar/render?action=TEMPLATE" +
               $"&text={Uri.EscapeDataString(title)}" +
               $"&dates={Uri.EscapeDataString($"{startText}/{endText}")}" +
               $"&details={Uri.EscapeDataString(details)}" +
               $"&ctz={Uri.EscapeDataString(timezone)}";
    }
}