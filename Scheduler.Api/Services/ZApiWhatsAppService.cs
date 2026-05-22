using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using Scheduler.Api.Options;
using Scheduler.Api.Services.Contracts;

namespace Scheduler.Api.Services;

public class ZApiWhatsAppService : IWhatsAppService
{
    private readonly HttpClient _httpClient;
    private readonly ZApiOptions _options;

    public ZApiWhatsAppService(HttpClient httpClient, IOptions<ZApiOptions> options)
    {
        _httpClient = httpClient;
        _options = options.Value;
    }

    public async Task<bool> SendTextAsync(string? phone, string message)
    {
        if (!_options.Enabled || string.IsNullOrWhiteSpace(phone) || string.IsNullOrWhiteSpace(message))
        {
            return false;
        }

        var digits = new string(phone.Where(char.IsDigit).ToArray());

        if (string.IsNullOrWhiteSpace(digits))
        {
            return false;
        }

        if (!digits.StartsWith(_options.DefaultCountryCode))
        {
            digits = $"{_options.DefaultCountryCode}{digits}";
        }

        var path = _options.SendTextPath
            .Replace("{instanceId}", _options.InstanceId)
            .Replace("{token}", _options.Token);

        var url = $"{_options.BaseUrl.TrimEnd('/')}{path}";

        var payload = new
        {
            phone = digits,
            message
        };

        var request = new HttpRequestMessage(HttpMethod.Post, url);
        request.Headers.Add("Client-Token", _options.ClientToken);
        request.Content = new StringContent(
            JsonSerializer.Serialize(payload),
            Encoding.UTF8,
            "application/json"
        );

        var response = await _httpClient.SendAsync(request);
        return response.IsSuccessStatusCode;
    }
}