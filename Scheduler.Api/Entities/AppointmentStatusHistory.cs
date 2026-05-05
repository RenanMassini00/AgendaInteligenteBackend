using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Scheduler.Api.Entities;

[Table("appointment_status_history")]
public class AppointmentStatusHistory
{
    [Key]
    [Column("id")]
    public ulong Id { get; set; }

    [Column("appointment_id")]
    public ulong AppointmentId { get; set; }

    [Column("previous_status")]
    public string? PreviousStatus { get; set; }

    [Column("new_status")]
    public string NewStatus { get; set; } = "scheduled";

    [Column("changed_by_user_id")]
    public ulong ChangedByUserId { get; set; }

    [Column("note")]
    public string? Note { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    public Appointment? Appointment { get; set; }
    public User? ChangedByUser { get; set; }
}
