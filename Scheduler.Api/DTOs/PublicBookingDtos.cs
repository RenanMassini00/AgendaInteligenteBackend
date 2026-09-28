using System.ComponentModel.DataAnnotations;

namespace Scheduler.Api.DTOs;

public record PublicBookingServiceResponse(
    ulong Id,
    ulong ProfessionalUserId,
    string Name,
    string? Description,
    int DurationMinutes,
    string Duration,
    decimal Price,
    string PriceFormatted
);

public record PublicBookingTeamMemberResponse(
    ulong Id,
    string FullName,
    string? Specialty
);

public record PublicBookAppointmentRequest(
    [Required] ulong ServiceId,
    [Required] string FullName,
    [Required] string Phone,
    string? Email,
    [Required] DateTime AppointmentDate,
    [Required] TimeSpan StartTime,
    [Required] TimeSpan EndTime,
    string? Notes,
    ulong? ProfessionalUserId = null
);

public record PublicBookingProfessionalResponse(
    ulong ProfessionalUserId,
    string DisplayName,
    string Subtitle,
    string PublicSlug,
    List<PublicBookingServiceResponse> Services,
    List<PublicBookingTeamMemberResponse> Professionals,
    string ThemeMode,
    string AccentColor,
    string? LogoUrl
)
{
    public string Theme => ThemeMode;
    public string? CompanyLogoUrl => LogoUrl;
}

public record PublicBookingAvailableSlotsResponse(
    string Date,
    ulong ServiceId,
    List<string> Slots,
    ulong ProfessionalUserId
);

public record PublicBookingRequest(
    string FullName,
    string Phone,
    ulong ServiceId,
    string Date,
    string Time,
    string? Email = null,
    ulong? ProfessionalUserId = null
);

public record PublicBookingCreatedResponse(
    ulong AppointmentId,
    string FullName,
    string Phone,
    string ServiceName,
    string Date,
    string Time,
    string Status,
    string Message,
    ulong ProfessionalUserId,
    string ProfessionalName
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
    string Message,
    ulong ProfessionalUserId
);
