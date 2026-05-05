namespace Scheduler.Api.DTOs;

public record ServiceResponse(
    ulong Id,
    string Name,
    string? Description,
    int DurationMinutes,
    string Duration,
    decimal Price,
    string PriceFormatted,
    string? ColorHex
);

public record ServiceCreateRequest(
    ulong UserId,
    string Name,
    string? Description,
    int DurationMinutes,
    decimal Price,
    string? ColorHex
);

public record ServiceUpdateRequest(
    string Name,
    string? Description,
    int DurationMinutes,
    decimal Price,
    string? ColorHex,
    bool IsActive
);
