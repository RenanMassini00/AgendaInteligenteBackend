using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Scheduler.Api.Entities;

[Table("user_settings")]
public class UserSetting
{
    [Key]
    [Column("id")]
    public ulong Id { get; set; }

    [Column("user_id")]
    public ulong UserId { get; set; }

    [ForeignKey(nameof(UserId))]
    public User? User { get; set; }

    [Column("theme")]
    public string Theme { get; set; } = "light";

    [Column("accent_color")]
    public string AccentColor { get; set; } = "blue";

    [Column("company_logo_url")]
    public string? CompanyLogoUrl { get; set; }

    [Column("google_calendar_id")]
    public string? GoogleCalendarId { get; set; }

    [Column("language_code")]
    public string LanguageCode { get; set; } = "pt-BR";

    [Column("reminder_minutes")]
    public int ReminderMinutes { get; set; } = 60;

    [Column("email_notifications")]
    public bool EmailNotifications { get; set; }

    [Column("whatsapp_notifications")]
    public bool WhatsappNotifications { get; set; } = true;

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; }
}