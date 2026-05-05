namespace Scheduler.Api.DTOs;

public record LoginRequest(string Email, string Password);

public record RegisterProfessionalRequest(
    string FullName,
    string? BusinessName,
    string Email,
    string? Phone,
    string Password,
    string? Specialty
);

public record RegisterClientRequest(
    ulong ProfessionalUserId,
    string FullName,
    string Email,
    string Phone,
    string Password,
    DateTime? BirthDate,
    string? Notes
);

public record UserResponse(
    ulong Id,
    string FullName,
    string? BusinessName,
    string Email,
    string? Phone,
    string? Specialty,
    string Timezone,
    string Role,
    ulong? ProfessionalUserId,
    ulong? ClientId,
    string? PublicSlug
);

public record LoginResponse(
    string Token,
    UserResponse User
);
