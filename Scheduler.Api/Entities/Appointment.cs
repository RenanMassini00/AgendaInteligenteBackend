using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Scheduler.Api.Entities;

[Table("appointments")]
public class Appointment
{
    [Key]
    [Column("id")]
    public ulong Id { get; set; }

    [Column("user_id")]
    public ulong UserId { get; set; }

    [Column("client_id")]
    public ulong ClientId { get; set; }

    [Column("service_id")]
    public ulong ServiceId { get; set; }

    [Column("appointment_date")]
    public DateTime AppointmentDate { get; set; }

    [Column("start_time")]
    public TimeSpan StartTime { get; set; }

    [Column("end_time")]
    public TimeSpan EndTime { get; set; }

    [Column("status")]
    public string Status { get; set; } = "scheduled";

    [Column("price_at_booking")]
    public decimal PriceAtBooking { get; set; }

    [Column("notes")]
    public string? Notes { get; set; }

    [Column("cancelled_reason")]
    public string? CancelledReason { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; }

    public User? User { get; set; }
    public Client? Client { get; set; }
    public Service? Service { get; set; }
}
