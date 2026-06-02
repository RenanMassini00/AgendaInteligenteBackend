using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using Scheduler.Api.Options;
using Scheduler.Api.Services.Contracts;

namespace Scheduler.Api.Services;

public class MetaWhatsAppService : IWhatsAppService
{
    private readonly HttpClient _httpClient;
    private readonly WhatsAppMetaOptions _options;
    private readonly ILogger<MetaWhatsAppService> _logger;

    public MetaWhatsAppService(
        HttpClient httpClient,
        IOptions<WhatsAppMetaOptions> options,
        ILogger<MetaWhatsAppService> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<bool> SendTextAsync(string? phone, string message)
    {
        if (!_options.Enabled)
        {
            _logger.LogWarning("WhatsApp Meta desabilitado.");
            return false;
        }

        if (string.IsNullOrWhiteSpace(_options.PhoneNumberId) ||
            string.IsNullOrWhiteSpace(_options.AccessToken))
        {
            _logger.LogWarning("Configuração do WhatsApp Meta incompleta.");
            return false;
        }

        if (string.IsNullOrWhiteSpace(phone) || string.IsNullOrWhiteSpace(message))
        {
            _logger.LogWarning("Telefone ou mensagem inválidos para WhatsApp.");
            return false;
        }

        try
        {
            var digits = NormalizePhone(phone);

            if (string.IsNullOrWhiteSpace(digits))
            {
                _logger.LogWarning("Telefone inválido: {Phone}", phone);
                return false;
            }

            if (!digits.StartsWith(_options.DefaultCountryCode))
            {
                digits = $"{_options.DefaultCountryCode}{digits}";
            }

            var url =
                $"{_options.BaseUrl.TrimEnd('/')}/{_options.ApiVersion}/{_options.PhoneNumberId}/messages";

            var payload = new
            {
                messaging_product = "whatsapp",
                to = digits,
                type = "text",
                text = new
                {
                    preview_url = false,
                    body = message
                }
            };

            using var request = new HttpRequestMessage(HttpMethod.Post, url);
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", _options.AccessToken);

            request.Content = new StringContent(
                JsonSerializer.Serialize(payload),
                Encoding.UTF8,
                "application/json"
            );

            using var response = await _httpClient.SendAsync(request);
            var content = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "Falha no envio WhatsApp Meta. Status: {StatusCode}. Retorno: {Content}",
                    response.StatusCode,
                    content
                );
                return false;
            }

            _logger.LogInformation("WhatsApp enviado com sucesso para {Phone}.", digits);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao enviar WhatsApp Meta para {Phone}.", phone);
            return false;
        }
    }

    private static string NormalizePhone(string phone)
    {
        return new string(phone.Where(char.IsDigit).ToArray());
    }
}