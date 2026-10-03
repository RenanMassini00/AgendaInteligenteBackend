using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Scheduler.Api.Entities;

[Table("appointment_payments")]
public class AppointmentPayment
{
    [Key]
    [Column("id")]
    public ulong Id { get; set; }

    [Column("appointment_id")]
    public ulong AppointmentId { get; set; }

    [Column("account_owner_user_id")]
    public ulong AccountOwnerUserId { get; set; }

    [Column("public_reference")]
    public Guid PublicReference { get; set; }

    [Column("provider_payment_id")]
    public string? ProviderPaymentId { get; set; }

    [Column("status")]
    public string Status { get; set; } = "pending";

    [Column("amount")]
    public decimal Amount { get; set; }

    [Column("qr_code")]
    public string? QrCode { get; set; }

    [Column("qr_code_base64")]
    public string? QrCodeBase64 { get; set; }

    [Column("expires_at")]
    public DateTime ExpiresAt { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; }
}
