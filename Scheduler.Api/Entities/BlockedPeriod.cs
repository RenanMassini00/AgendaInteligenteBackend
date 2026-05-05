using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Scheduler.Api.Entities;

[Table("blocked_periods")]
public class BlockedPeriod
{
    [Key]
    [Column("id")]
    public ulong Id { get; set; }

    [Column("user_id")]
    public ulong UserId { get; set; }

    [Column("start_datetime")]
    public DateTime StartDatetime { get; set; }

    [Column("end_datetime")]
    public DateTime EndDatetime { get; set; }

    [Column("reason")]
    public string? Reason { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; }

    public User? User { get; set; }
}
