namespace Scheduler.Api.DTOs;

public record SettingsResponse(
    ulong UserId,
    string Theme,
    string AccentColor,
    string? CompanyLogoUrl
);

public record AdminBrandingUserResponse(
    ulong UserId,
    string FullName,
    string? BusinessName,
    string Email,
    string Theme,
    string AccentColor,
    string? CompanyLogoUrl
);

public record AdminBrandingUpdateRequest(
    string Theme,
    string AccentColor,
    string? CompanyLogoUrl
);