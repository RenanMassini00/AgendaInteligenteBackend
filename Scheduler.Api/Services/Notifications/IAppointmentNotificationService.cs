using Scheduler.Api.Entities;

namespace Scheduler.Api.Services.Notifications;

public interface IAppointmentNotificationService
{
    Task NotifyCreatedAsync(User professional, Client client, Service service, Appointment appointment, CancellationToken cancellationToken = default);
    Task NotifyUpdatedAsync(User professional, Client client, Service service, Appointment appointment, CancellationToken cancellationToken = default);
    Task NotifyCancelledAsync(User professional, Client client, Service service, Appointment appointment, CancellationToken cancellationToken = default);
    Task NotifyCompletedAsync(User professional, Client client, Service service, Appointment appointment, CancellationToken cancellationToken = default);
}