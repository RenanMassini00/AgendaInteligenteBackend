using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;

namespace Scheduler.Api.Services.Notifications;

public class HttpWhatsAppGateway : IWhatsAppGateway
{
    private readonly HttpClient _httpClient;
    private readonly WhatsAppOptions _options;

    public HttpWhatsAppGateway(HttpClient httpClient, IOptions<WhatsAppOptions> options)
    {
        _httpClient = httpClient;
        _options = options.Value;
    }

    public async Task SendTextAsync(string phone, string message, CancellationToken cancellationToken = default)
    {
        if (!_options.Enabled)
        {
            return;
        }

        var normalizedPhone = NormalizePhone(phone, _options.DefaultCountryCode);
        if (string.IsNullOrWhiteSpace(normalizedPhone))
        {
            return;
        }

        if (!string.IsNullOrWhiteSpace(_options.Token))
        {
            _httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", _options.Token);
        }

        var payload = new
        {
            to = normalizedPhone,
            message
        };

        var content = new StringContent(
            JsonSerializer.Serialize(payload),
            Encoding.UTF8,
            "application/json");

        var response = await _httpClient.PostAsync(_options.SendPath, content, cancellationToken);

        response.EnsureSuccessStatusCode();
    }

    private static string NormalizePhone(string? rawPhone, string defaultCountryCode)
    {
        if (string.IsNullOrWhiteSpace(rawPhone))
        {
            return string.Empty;
        }

        var digits = new string(rawPhone.Where(char.IsDigit).ToArray());

        if (string.IsNullOrWhiteSpace(digits))
        {
            return string.Empty;
        }

        if (!digits.StartsWith(defaultCountryCode))
        {
            digits = defaultCountryCode + digits;
        }

        return digits;
    }
}