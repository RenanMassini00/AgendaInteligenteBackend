using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Scheduler.Api.Entities;

[Table("app_notifications")]
public class AppNotification
{
    [Key]
    [Column("id")]
    public ulong Id { get; set; }

    [Column("user_id")]
    public ulong UserId { get; set; }

    [Column("appointment_id")]
    public ulong? AppointmentId { get; set; }

    [Column("type")]
    public string Type { get; set; } = "appointment";

    [Column("title")]
    public string Title { get; set; } = string.Empty;

    [Column("message")]
    public string Message { get; set; } = string.Empty;

    [Column("action_url")]
    public string? ActionUrl { get; set; }

    [Column("calendar_url")]
    public string? CalendarUrl { get; set; }

    [Column("is_read")]
    public bool IsRead { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [Column("read_at")]
    public DateTime? ReadAt { get; set; }

    public User? User { get; set; }
    public Appointment? Appointment { get; set; }
}
