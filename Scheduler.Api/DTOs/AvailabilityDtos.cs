namespace Scheduler.Api.DTOs;

public record AvailabilityResponse(
    ulong Id,
    int Weekday,
    string WeekdayName,
    string StartTime,
    string EndTime,
    bool IsActive
);

public record AvailabilityCreateRequest(
    ulong UserId,
    int Weekday,
    string StartTime,
    string EndTime,
    bool IsActive
);

public record AvailabilityUpdateRequest(
    int Weekday,
    string StartTime,
    string EndTime,
    bool IsActive
);

public record CreateAvailabilityDateRequest(
    DateTime AvailableDate,
    string StartTime,
    string EndTime
);

public record UpdateAvailabilityDateRequest(
    DateTime AvailableDate,
    string StartTime,
    string EndTime
);

public record AvailabilityDateResponse(
    ulong Id,
    string AvailableDate,
    string StartTime,
    string EndTime
);