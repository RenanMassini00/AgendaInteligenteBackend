namespace Scheduler.Api.DTOs;

public record PublicBookingServiceResponse(
    ulong Id,
    string Name,
    string? Description,
    int DurationMinutes,
    string Duration,
    decimal Price,
    string PriceFormatted
);

public record PublicBookingProfessionalResponse(
    string DisplayName,
    string Subtitle,
    string PublicSlug,
    List<PublicBookingServiceResponse> Services
);

public record PublicBookingAvailableSlotsResponse(
    string Date,
    ulong ServiceId,
    List<string> Slots
);

public record PublicBookingRequest(
    string FullName,
    string Phone,
    ulong ServiceId,
    string Date,
    string Time
);

public record PublicBookingCreatedResponse(
    ulong AppointmentId,
    string FullName,
    string Phone,
    string ServiceName,
    string Date,
    string Time,
    string Status,
    string Message
);