using Scheduler.Api.DTOs;
using Scheduler.Api.Entities;

namespace Scheduler.Api.Services.Contracts;

public interface IPushNotificationService
{
    Task<bool> RegisterSubscriptionAsync(
        ulong userId,
        PushSubscriptionRegisterRequest request,
        CancellationToken cancellationToken = default);

    Task<bool> RemoveSubscriptionAsync(
        ulong userId,
        string endpoint,
        CancellationToken cancellationToken = default);

    Task<bool> SendToUserAsync(
        ulong userId,
        string title,
        string body,
        string? url = null,
        string? tag = null,
        ulong? appointmentId = null,
        CancellationToken cancellationToken = default);

    Task<(bool ClientPushSent, bool ProfessionalPushSent)> SendAppointmentCreatedAsync(
        User professional,
        Client client,
        Service service,
        Appointment appointment,
        CancellationToken cancellationToken = default);
}
