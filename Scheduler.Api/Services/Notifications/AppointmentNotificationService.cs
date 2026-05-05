using Scheduler.Api.Entities;

namespace Scheduler.Api.Services.Notifications;

public class AppointmentNotificationService : IAppointmentNotificationService
{
    private readonly IWhatsAppGateway _whatsAppGateway;

    public AppointmentNotificationService(IWhatsAppGateway whatsAppGateway)
    {
        _whatsAppGateway = whatsAppGateway;
    }

    public async Task NotifyCreatedAsync(User professional, Client client, Service service, Appointment appointment, CancellationToken cancellationToken = default)
    {
        var professionalName = GetProfessionalDisplayName(professional);
        var when = GetWhenLabel(appointment);

        var professionalMessage =
            $"Novo agendamento recebido.\n" +
            $"Cliente: {client.FullName}\n" +
            $"Telefone: {client.Phone}\n" +
            $"Serviço: {service.Name}\n" +
            $"Quando: {when}\n" +
            $"Valor: {appointment.PriceAtBooking:C}";

        var clientMessage =
            $"Olá, {client.FullName}! Seu agendamento foi confirmado.\n" +
            $"Profissional: {professionalName}\n" +
            $"Serviço: {service.Name}\n" +
            $"Quando: {when}\n" +
            $"Valor: {appointment.PriceAtBooking:C}";

        await SendToBothAsync(professional.Phone, professionalMessage, client.Phone, clientMessage, cancellationToken);
    }

    public async Task NotifyUpdatedAsync(User professional, Client client, Service service, Appointment appointment, CancellationToken cancellationToken = default)
    {
        var professionalName = GetProfessionalDisplayName(professional);
        var when = GetWhenLabel(appointment);

        var professionalMessage =
            $"Agendamento alterado.\n" +
            $"Cliente: {client.FullName}\n" +
            $"Serviço: {service.Name}\n" +
            $"Novo horário: {when}\n" +
            $"Valor: {appointment.PriceAtBooking:C}";

        var clientMessage =
            $"Olá, {client.FullName}! Seu agendamento foi alterado.\n" +
            $"Profissional: {professionalName}\n" +
            $"Serviço: {service.Name}\n" +
            $"Novo horário: {when}\n" +
            $"Valor: {appointment.PriceAtBooking:C}";

        await SendToBothAsync(professional.Phone, professionalMessage, client.Phone, clientMessage, cancellationToken);
    }

    public async Task NotifyCancelledAsync(User professional, Client client, Service service, Appointment appointment, CancellationToken cancellationToken = default)
    {
        var professionalName = GetProfessionalDisplayName(professional);
        var when = GetWhenLabel(appointment);

        var professionalMessage =
            $"Agendamento cancelado.\n" +
            $"Cliente: {client.FullName}\n" +
            $"Serviço: {service.Name}\n" +
            $"Horário: {when}";

        var clientMessage =
            $"Olá, {client.FullName}. Seu agendamento foi cancelado.\n" +
            $"Profissional: {professionalName}\n" +
            $"Serviço: {service.Name}\n" +
            $"Horário: {when}";

        await SendToBothAsync(professional.Phone, professionalMessage, client.Phone, clientMessage, cancellationToken);
    }

    public async Task NotifyCompletedAsync(User professional, Client client, Service service, Appointment appointment, CancellationToken cancellationToken = default)
    {
        var professionalName = GetProfessionalDisplayName(professional);
        var when = GetWhenLabel(appointment);

        var professionalMessage =
            $"Agendamento concluído.\n" +
            $"Cliente: {client.FullName}\n" +
            $"Serviço: {service.Name}\n" +
            $"Horário: {when}\n" +
            $"Valor recebido: {appointment.PriceAtBooking:C}";

        var clientMessage =
            $"Olá, {client.FullName}! Seu atendimento foi concluído.\n" +
            $"Profissional: {professionalName}\n" +
            $"Serviço: {service.Name}\n" +
            $"Horário: {when}\n" +
            $"Obrigado pela preferência.";

        await SendToBothAsync(professional.Phone, professionalMessage, client.Phone, clientMessage, cancellationToken);
    }

    private async Task SendToBothAsync(
        string? professionalPhone,
        string professionalMessage,
        string? clientPhone,
        string clientMessage,
        CancellationToken cancellationToken)
    {
        var tasks = new List<Task>();

        if (!string.IsNullOrWhiteSpace(professionalPhone))
        {
            tasks.Add(_whatsAppGateway.SendTextAsync(professionalPhone, professionalMessage, cancellationToken));
        }

        if (!string.IsNullOrWhiteSpace(clientPhone))
        {
            tasks.Add(_whatsAppGateway.SendTextAsync(clientPhone, clientMessage, cancellationToken));
        }

        await Task.WhenAll(tasks);
    }

    private static string GetProfessionalDisplayName(User professional)
    {
        return !string.IsNullOrWhiteSpace(professional.BusinessName)
            ? professional.BusinessName
            : professional.FullName;
    }

    private static string GetWhenLabel(Appointment appointment)
    {
        return $"{appointment.AppointmentDate:dd/MM/yyyy} às {appointment.StartTime:hh\\:mm}";
    }
}