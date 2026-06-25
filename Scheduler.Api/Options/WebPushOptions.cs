namespace Scheduler.Api.Options;

public class WebPushOptions
{
    public bool Enabled { get; set; }
    public string Subject { get; set; } = string.Empty;
    public string PublicKey { get; set; } = string.Empty;
    public string PrivateKey { get; set; } = string.Empty;
    public string DefaultUrl { get; set; } = "/";
    public string ClientUrl { get; set; } = "/client/appointments";
    public string ProfessionalUrl { get; set; } = "/appointments";
    public string? IconUrl { get; set; }
    public string? BadgeUrl { get; set; }
    public int TtlSeconds { get; set; } = 86400;
}
