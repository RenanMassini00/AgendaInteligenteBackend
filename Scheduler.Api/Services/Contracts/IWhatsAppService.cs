namespace Scheduler.Api.Services.Contracts;

public interface IWhatsAppService
{
    Task<bool> SendTextAsync(string? phone, string message);
}