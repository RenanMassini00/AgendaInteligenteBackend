namespace Scheduler.Api.Options;

public class ZApiOptions
{
    public bool Enabled { get; set; }
    public string BaseUrl { get; set; } = string.Empty;
    public string InstanceId { get; set; } = string.Empty;
    public string Token { get; set; } = string.Empty;
    public string ClientToken { get; set; } = string.Empty;
    public string SendTextPath { get; set; } = "/instances/{instanceId}/token/{token}/send-text";
    public string DefaultCountryCode { get; set; } = "55";
}