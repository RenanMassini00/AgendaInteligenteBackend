namespace Scheduler.Api.Options;

public class GoogleCalendarOptions
{
    public bool Enabled { get; set; }
    public string ApplicationName { get; set; } = string.Empty;
    public string ServiceAccountJsonPath { get; set; } = string.Empty;
    public string DefaultTimezone { get; set; } = "America/Sao_Paulo";
}