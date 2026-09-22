namespace Scheduler.Api.DTOs;

public record NotificationResponse(
    ulong Id,
    ulong? AppointmentId,
    string Type,
    string Title,
    string Message,
    string? ActionUrl,
    string? CalendarUrl,
    bool IsRead,
    DateTime CreatedAt
);

public record AppointmentResponseRequest(string Action);
