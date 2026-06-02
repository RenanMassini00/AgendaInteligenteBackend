namespace Scheduler.Api.Options;

public class WhatsAppMetaOptions
{
    public bool Enabled { get; set; }
    public string ApiVersion { get; set; } = "v20.0";
    public string BaseUrl { get; set; } = "https://graph.facebook.com";
    public string PhoneNumberId { get; set; } = string.Empty;
    public string AccessToken { get; set; } = string.Empty;
    public string VerifyToken { get; set; } = string.Empty;
    public string DefaultCountryCode { get; set; } = "55";
}