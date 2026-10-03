namespace Scheduler.Api.Options;

public class MercadoPagoOptions
{
    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
    public string WebhookSecret { get; set; } = string.Empty;
    public string WebhookUrl { get; set; } = string.Empty;
    public string OAuthCallbackUrl { get; set; } = string.Empty;
    public string TokenEncryptionKey { get; set; } = string.Empty;
    public decimal DepositPercentage { get; set; } = 20;
    public int HoldMinutes { get; set; } = 30;
}
