namespace Scheduler.Api.DTOs;

public record LoginRequest(string Email, string Password);

public record RegisterProfessionalRequest(
    string FullName,
    string? BusinessName,
    string Email,
    string? Phone,
    string Password,
    string? Specialty,
    string? Timezone,
    string? PublicSlug,
    bool HasAppointmentsModule,
    bool HasCatalogModule
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
    string? PublicSlug,
    ulong? ProfessionalUserId,
    ulong? ClientId,
    bool HasAppointmentsModule,
    bool HasCatalogModule,
    string ThemeMode,
    string AccentColor,
    string? LogoUrl
);

public record LoginResponse(
    string Token,
    UserResponse User
);