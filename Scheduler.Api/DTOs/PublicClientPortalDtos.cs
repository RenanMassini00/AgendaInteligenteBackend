namespace Scheduler.Api.DTOs;

public record PublicProfessionalResponse(
    ulong Id,
    string FullName,
    string? BusinessName,
    string Email,
    string? Specialty,
    string? PublicSlug
);

public record AvailableSlotResponse(
    string Time,
    string EndTime
);

public record ClientPortalAppointmentCreateRequest(
    ulong ProfessionalUserId,
    ulong ServiceId,
    string Date,
    string Time,
    string? Notes
);
