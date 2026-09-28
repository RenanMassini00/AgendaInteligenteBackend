namespace Scheduler.Api.DTOs;

public record ProfessionalEmployeeCreateRequest(
    string FullName,
    string Email,
    string Password,
    string? Phone,
    string? Specialty,
    string? Timezone
);

public record ProfessionalEmployeeUpdateRequest(
    string FullName,
    string Email,
    string? Phone,
    string? Specialty,
    string? Timezone,
    string? Password,
    bool IsActive
);

public record ProfessionalEmployeeResponse(
    ulong Id,
    string FullName,
    string Email,
    string? Phone,
    string? Specialty,
    string Timezone,
    bool IsActive,
    ulong TeamOwnerUserId
);
