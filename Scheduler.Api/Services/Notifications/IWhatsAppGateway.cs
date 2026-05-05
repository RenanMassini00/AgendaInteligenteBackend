namespace Scheduler.Api.Services.Notifications;

public interface IWhatsAppGateway
{
    Task SendTextAsync(string phone, string message, CancellationToken cancellationToken = default);
}