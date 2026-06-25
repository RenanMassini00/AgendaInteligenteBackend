using System.ComponentModel.DataAnnotations;

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

public record PublicBookAppointmentRequest(
    [Required] ulong ServiceId,
    [Required] string FullName,
    [Required] string Phone,
    string? Email,
    [Required] DateTime AppointmentDate,
    [Required] TimeSpan StartTime,
    [Required] TimeSpan EndTime,
    string? Notes
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

public record PublicBookingSuccessResponse(
    ulong AppointmentId,
    string ClientName,
    string ServiceName,
    string Date,
    string StartTime,
    string EndTime,
    string ProfessionalName,
    string? BusinessName,
    bool ClientEmailSent,
    bool ProfessionalEmailSent,
    bool ClientWhatsAppSent,
    bool ProfessionalWhatsAppSent,
    bool ClientPushSent,
    bool ProfessionalPushSent,
    bool CalendarCreated,
    string Message
);
