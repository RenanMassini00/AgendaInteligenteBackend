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

    [Column("theme")]
    public string Theme { get; set; } = "light";

    [Column("language_code")]
    public string LanguageCode { get; set; } = "pt-BR";

    [Column("reminder_minutes")]
    public int ReminderMinutes { get; set; } = 60;

    [Column("email_notifications")]
    public bool EmailNotifications { get; set; }

    [Column("whatsapp_notifications")]
    public bool WhatsappNotifications { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; }

    public User? User { get; set; }
}
