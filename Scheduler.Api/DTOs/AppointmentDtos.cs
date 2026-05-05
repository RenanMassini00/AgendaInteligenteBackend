namespace Scheduler.Api.DTOs;

public record AppointmentResponse(
    ulong Id,
    ulong ClientId,
    ulong ServiceId,
    string ClientName,
    string ServiceName,
    string Date,
    string Time,
    string StartTime,
    string EndTime,
    string Status,
    decimal Price,
    string PriceFormatted,
    string? Notes
);

public record AppointmentCreateRequest(
    ulong UserId,
    ulong ClientId,
    ulong ServiceId,
    string Date,
    string Time,
    string Status,
    string? Notes
);

public record AppointmentStatusUpdateRequest(
    string Status,
    string? CancelledReason
);

public record AppointmentUpdateRequest(
    ulong UserId,
    ulong ClientId,
    ulong ServiceId,
    string Date,
    string Time,
    string Status,
    string? Notes
);
