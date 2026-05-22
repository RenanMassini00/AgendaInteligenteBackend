namespace Scheduler.Api.Services.Contracts;

public interface IEmailService
{
    Task<bool> SendAsync(string? toEmail, string subject, string htmlBody);
}