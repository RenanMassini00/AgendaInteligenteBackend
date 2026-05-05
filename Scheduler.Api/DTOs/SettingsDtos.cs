namespace Scheduler.Api.DTOs;

public record SettingsResponse(
    ulong Id,
    ulong UserId,
    string Theme,
    string LanguageCode,
    int ReminderMinutes,
    bool EmailNotifications,
    bool WhatsappNotifications
);

public record SettingsUpdateRequest(
    string Theme,
    string LanguageCode,
    int ReminderMinutes,
    bool EmailNotifications,
    bool WhatsappNotifications
);
