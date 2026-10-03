using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Scheduler.Api.Entities;

[Table("mercado_pago_accounts")]
public class MercadoPagoAccount
{
    [Key]
    [Column("user_id")]
    public ulong UserId { get; set; }

    [Column("mercado_pago_user_id")]
    public string MercadoPagoUserId { get; set; } = string.Empty;

    [Column("access_token_encrypted")]
    public string AccessTokenEncrypted { get; set; } = string.Empty;

    [Column("refresh_token_encrypted")]
    public string RefreshTokenEncrypted { get; set; } = string.Empty;

    [Column("expires_at")]
    public DateTime ExpiresAt { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; }
}
