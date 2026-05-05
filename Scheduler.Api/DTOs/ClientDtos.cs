namespace Scheduler.Api.DTOs;

public record ClientResponse(
    ulong Id,
    string Name,
    string? Email,
    string Phone,
    DateTime? BirthDate,
    string? Notes
);

public record ClientCreateRequest(
    ulong UserId,
    string Name,
    string? Email,
    string Phone,
    DateTime? BirthDate,
    string? Notes
);

public record ClientUpdateRequest(
    string Name,
    string? Email,
    string Phone,
    DateTime? BirthDate,
    string? Notes,
    bool IsActive
);
