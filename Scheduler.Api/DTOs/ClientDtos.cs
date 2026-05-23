namespace Scheduler.Api.DTOs;

public record ClientResponse(
    ulong Id,
    string FullName,
    string? Email,
    string Phone,
    string? BirthDate,
    string? Notes,
    string Status,
    string CreatedAt
);

public record ClientCreateRequest(
    string FullName,
    string? Email,
    string Phone,
    DateTime? BirthDate,
    string? Notes
);

public record ClientUpdateRequest(
    string FullName,
    string? Email,
    string Phone,
    DateTime? BirthDate,
    string? Notes
);
