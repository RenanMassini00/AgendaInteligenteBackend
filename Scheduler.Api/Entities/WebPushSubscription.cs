using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Scheduler.Api.Entities;

[Table("push_subscriptions")]
public class WebPushSubscription
{
    [Key]
    [Column("id")]
    public ulong Id { get; set; }

    [Column("user_id")]
    public ulong UserId { get; set; }

    [Column("endpoint_hash")]
    public string EndpointHash { get; set; } = string.Empty;

    [Column("endpoint")]
    public string Endpoint { get; set; } = string.Empty;

    [Column("p256dh")]
    public string P256dh { get; set; } = string.Empty;

    [Column("auth")]
    public string Auth { get; set; } = string.Empty;

    [Column("expiration_time")]
    public long? ExpirationTime { get; set; }

    [Column("user_agent")]
    public string? UserAgent { get; set; }

    [Column("device_name")]
    public string? DeviceName { get; set; }

    [Column("is_active")]
    public bool IsActive { get; set; } = true;

    [Column("failure_count")]
    public int FailureCount { get; set; }

    [Column("last_success_at")]
    public DateTime? LastSuccessAt { get; set; }

    [Column("last_failure_at")]
    public DateTime? LastFailureAt { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; }

    public User? User { get; set; }
}
