namespace Scheduler.Api.Services.Notifications;

public class WhatsAppOptions
{
    public bool Enabled { get; set; }
    public string BaseUrl { get; set; } = string.Empty;
    public string Token { get; set; } = string.Empty;
    public string SendPath { get; set; } = "/messages";
    public string DefaultCountryCode { get; set; } = "55";
}
