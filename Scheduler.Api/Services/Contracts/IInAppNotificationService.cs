using Scheduler.Api.Entities;

namespace Scheduler.Api.Services.Contracts;

public interface IInAppNotificationService
{
    Task CreateAppointmentNotificationsAsync(
        User professional,
        Client client,
        Service service,
        Appointment appointment,
        CancellationToken cancellationToken = default);

    Task NotifyAppointmentResponseAsync(
        User professional,
        Client client,
        Service service,
        Appointment appointment,
        string action,
        CancellationToken cancellationToken = default);
}
