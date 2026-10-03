using System.Collections.Concurrent;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Scheduler.Api.Data;
using Scheduler.Api.Entities;
using Scheduler.Api.Options;
using Scheduler.Api.Services.Contracts;

namespace Scheduler.Api.Services;

public sealed class MercadoPagoService : IMercadoPagoService
{
    private static readonly ConcurrentDictionary<ulong, SemaphoreSlim> RefreshLocks = new();
    private readonly AppDbContext _context;
    private readonly HttpClient _httpClient;
    private readonly MercadoPagoOptions _options;
    private readonly ILogger<MercadoPagoService> _logger;

    public MercadoPagoService(
        AppDbContext context,
        HttpClient httpClient,
        IOptions<MercadoPagoOptions> options,
        ILogger<MercadoPagoService> logger)
    {
        _context = context;
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;
    }

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(_options.ClientId) &&
        !string.IsNullOrWhiteSpace(_options.ClientSecret) &&
        !string.IsNullOrWhiteSpace(_options.WebhookSecret) &&
        IsHttpsUrl(_options.WebhookUrl) &&
        IsHttpsUrl(_options.OAuthCallbackUrl) &&
        TryGetEncryptionKey(out _) &&
        _options.DepositPercentage is > 0 and <= 100 &&
        _options.HoldMinutes > 0;

    public decimal DepositPercentage => _options.DepositPercentage;
    public int HoldMinutes => _options.HoldMinutes;

    public async Task<string> CreateAuthorizationUrlAsync(
        ulong accountOwnerUserId,
        CancellationToken cancellationToken = default)
    {
        EnsureConfigured();

        var state = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
        var now = DateTime.UtcNow;

        var oldStates = await _context.MercadoPagoOAuthStates
            .Where(x => x.UserId == accountOwnerUserId || x.ExpiresAt <= now)
            .ToListAsync(cancellationToken);
        _context.MercadoPagoOAuthStates.RemoveRange(oldStates);
        _context.MercadoPagoOAuthStates.Add(new MercadoPagoOAuthState
        {
            UserId = accountOwnerUserId,
            StateHash = HashState(state),
            ExpiresAt = now.AddMinutes(10),
            CreatedAt = now
        });
        await _context.SaveChangesAsync(cancellationToken);

        var authorizationUrl =
            "https://auth.mercadopago.com/authorization" +
            $"?client_id={Uri.EscapeDataString(_options.ClientId)}" +
            "&response_type=code" +
            "&platform_id=mp" +
            $"&state={Uri.EscapeDataString(state)}" +
            $"&redirect_uri={Uri.EscapeDataString(_options.OAuthCallbackUrl)}";

        return authorizationUrl;
    }

    public async Task CompleteAuthorizationAsync(
        string code,
        string state,
        CancellationToken cancellationToken = default)
    {
        EnsureConfigured();

        var stateHash = HashState(state);
        var authorizationState = await _context.MercadoPagoOAuthStates
            .FirstOrDefaultAsync(
                x => x.StateHash == stateHash && x.ExpiresAt > DateTime.UtcNow,
                cancellationToken);

        if (authorizationState is null)
            throw new MercadoPagoApiException("A autorização do Mercado Pago expirou ou não é válida.");

        _context.MercadoPagoOAuthStates.Remove(authorizationState);
        await _context.SaveChangesAsync(cancellationToken);

        var token = await RequestTokenAsync(
            new Dictionary<string, string>
            {
                ["grant_type"] = "authorization_code",
                ["client_id"] = _options.ClientId,
                ["client_secret"] = _options.ClientSecret,
                ["code"] = code,
                ["redirect_uri"] = _options.OAuthCallbackUrl
            },
            cancellationToken);

        if (string.IsNullOrWhiteSpace(token.AccessToken) ||
            string.IsNullOrWhiteSpace(token.RefreshToken) ||
            string.IsNullOrWhiteSpace(token.UserId))
        {
            throw new MercadoPagoApiException("O Mercado Pago não retornou os dados esperados da conta.");
        }

        var account = await _context.MercadoPagoAccounts
            .FirstOrDefaultAsync(x => x.UserId == authorizationState.UserId, cancellationToken);
        var now = DateTime.UtcNow;
        if (account is null)
        {
            account = new MercadoPagoAccount
            {
                UserId = authorizationState.UserId,
                CreatedAt = now
            };
            _context.MercadoPagoAccounts.Add(account);
        }

        account.MercadoPagoUserId = token.UserId;
        account.AccessTokenEncrypted = EncryptToken(token.AccessToken);
        account.RefreshTokenEncrypted = EncryptToken(token.RefreshToken);
        account.ExpiresAt = now.AddSeconds(token.ExpiresIn);
        account.UpdatedAt = now;
        await _context.SaveChangesAsync(cancellationToken);
    }

    public Task<bool> HasConnectedAccountAsync(
        ulong accountOwnerUserId,
        CancellationToken cancellationToken = default)
    {
        return _context.MercadoPagoAccounts
            .AsNoTracking()
            .AnyAsync(x => x.UserId == accountOwnerUserId, cancellationToken);
    }

    public async Task<MercadoPagoPixPayment> CreatePixPaymentAsync(
        ulong accountOwnerUserId,
        ulong appointmentId,
        string payerEmail,
        decimal amount,
        DateTime expiresAt,
        string idempotencyKey,
        CancellationToken cancellationToken = default)
    {
        EnsureConfigured();
        var accessToken = await GetValidAccessTokenAsync(accountOwnerUserId, cancellationToken);

        using var request = new HttpRequestMessage(HttpMethod.Post, "v1/payments");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        request.Headers.Add("X-Idempotency-Key", idempotencyKey);
        request.Content = JsonContent.Create(new CreatePaymentRequest(
            amount,
            "pix",
            $"Sinal do agendamento #{appointmentId}",
            appointmentId.ToString(),
            _options.WebhookUrl,
            expiresAt.ToUniversalTime().ToString("yyyy-MM-dd'T'HH:mm:ss.fff'Z'"),
            new Payer(payerEmail)
        ));

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        var payment = await response.Content.ReadFromJsonAsync<CreatePaymentResponse>(
            cancellationToken: cancellationToken);

        if (!response.IsSuccessStatusCode ||
            payment is null ||
            string.IsNullOrWhiteSpace(payment.Id) ||
            string.IsNullOrWhiteSpace(payment.Status))
        {
            _logger.LogError(
                "Mercado Pago rejected Pix payment creation for appointment {AppointmentId}. HTTP {StatusCode}",
                appointmentId,
                (int)response.StatusCode);
            throw new MercadoPagoApiException("Não foi possível gerar o pagamento Pix.");
        }

        return new MercadoPagoPixPayment(
            payment.Id,
            payment.Status,
            payment.PointOfInteraction?.TransactionData?.QrCode,
            payment.PointOfInteraction?.TransactionData?.QrCodeBase64
        );
    }

    public async Task<MercadoPagoPaymentDetails> GetPaymentAsync(
        ulong accountOwnerUserId,
        string paymentId,
        CancellationToken cancellationToken = default)
    {
        EnsureConfigured();
        var accessToken = await GetValidAccessTokenAsync(accountOwnerUserId, cancellationToken);

        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            $"v1/payments/{Uri.EscapeDataString(paymentId)}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        var payment = await response.Content.ReadFromJsonAsync<GetPaymentResponse>(
            cancellationToken: cancellationToken);

        if (!response.IsSuccessStatusCode ||
            payment is null ||
            string.IsNullOrWhiteSpace(payment.Id) ||
            string.IsNullOrWhiteSpace(payment.Status) ||
            string.IsNullOrWhiteSpace(payment.ExternalReference) ||
            string.IsNullOrWhiteSpace(payment.CollectorId))
        {
            _logger.LogError(
                "Mercado Pago payment lookup failed for payment {PaymentId}. HTTP {StatusCode}",
                paymentId,
                (int)response.StatusCode);
            throw new MercadoPagoApiException("Não foi possível validar o pagamento recebido.");
        }

        return new MercadoPagoPaymentDetails(
            payment.Id,
            payment.Status,
            payment.ExternalReference,
            payment.TransactionAmount,
            payment.CurrencyId,
            payment.CollectorId
        );
    }

    private async Task<string> GetValidAccessTokenAsync(
        ulong accountOwnerUserId,
        CancellationToken cancellationToken)
    {
        var refreshLock = RefreshLocks.GetOrAdd(accountOwnerUserId, _ => new SemaphoreSlim(1, 1));
        await refreshLock.WaitAsync(cancellationToken);

        try
        {
            var account = await _context.MercadoPagoAccounts
                .FirstOrDefaultAsync(x => x.UserId == accountOwnerUserId, cancellationToken);
            if (account is null)
                throw new MercadoPagoApiException("Esta empresa ainda não conectou uma conta do Mercado Pago.");

            if (account.ExpiresAt > DateTime.UtcNow.AddMinutes(5))
                return DecryptToken(account.AccessTokenEncrypted);

            var refreshToken = DecryptToken(account.RefreshTokenEncrypted);
            var refreshedToken = await RequestTokenAsync(
                new Dictionary<string, string>
                {
                    ["grant_type"] = "refresh_token",
                    ["client_id"] = _options.ClientId,
                    ["client_secret"] = _options.ClientSecret,
                    ["refresh_token"] = refreshToken
                },
                cancellationToken);

            if (string.IsNullOrWhiteSpace(refreshedToken.AccessToken) ||
                string.IsNullOrWhiteSpace(refreshedToken.RefreshToken))
            {
                throw new MercadoPagoApiException("Não foi possível renovar a conexão do Mercado Pago.");
            }

            account.AccessTokenEncrypted = EncryptToken(refreshedToken.AccessToken);
            account.RefreshTokenEncrypted = EncryptToken(refreshedToken.RefreshToken);
            account.ExpiresAt = DateTime.UtcNow.AddSeconds(refreshedToken.ExpiresIn);
            account.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync(cancellationToken);

            return refreshedToken.AccessToken;
        }
        finally
        {
            refreshLock.Release();
        }
    }

    private async Task<TokenResponse> RequestTokenAsync(
        Dictionary<string, string> parameters,
        CancellationToken cancellationToken)
    {
        using var content = new FormUrlEncodedContent(parameters);
        using var response = await _httpClient.PostAsync("oauth/token", content, cancellationToken);
        var token = await response.Content.ReadFromJsonAsync<TokenResponse>(
            cancellationToken: cancellationToken);

        if (!response.IsSuccessStatusCode || token is null)
        {
            _logger.LogError(
                "Mercado Pago OAuth request failed. HTTP {StatusCode}",
                (int)response.StatusCode);
            throw new MercadoPagoApiException("Não foi possível concluir a conexão com o Mercado Pago.");
        }

        return token;
    }

    private string EncryptToken(string token)
    {
        if (!TryGetEncryptionKey(out var key))
            throw new MercadoPagoApiException("A chave de criptografia do Mercado Pago não está configurada.");

        var nonce = RandomNumberGenerator.GetBytes(12);
        var plaintext = Encoding.UTF8.GetBytes(token);
        var ciphertext = new byte[plaintext.Length];
        var tag = new byte[16];
        using (var aes = new AesGcm(key, tag.Length))
            aes.Encrypt(nonce, plaintext, ciphertext, tag);

        return string.Join(
            '.',
            Convert.ToBase64String(nonce),
            Convert.ToBase64String(tag),
            Convert.ToBase64String(ciphertext));
    }

    private string DecryptToken(string encryptedToken)
    {
        if (!TryGetEncryptionKey(out var key))
            throw new MercadoPagoApiException("A chave de criptografia do Mercado Pago não está configurada.");

        var parts = encryptedToken.Split('.');
        if (parts.Length != 3)
            throw new MercadoPagoApiException("A credencial conectada do Mercado Pago está inválida.");

        var nonce = Convert.FromBase64String(parts[0]);
        var tag = Convert.FromBase64String(parts[1]);
        var ciphertext = Convert.FromBase64String(parts[2]);
        var plaintext = new byte[ciphertext.Length];
        using (var aes = new AesGcm(key, tag.Length))
            aes.Decrypt(nonce, ciphertext, tag, plaintext);

        return Encoding.UTF8.GetString(plaintext);
    }

    private bool TryGetEncryptionKey(out byte[] key)
    {
        try
        {
            key = Convert.FromBase64String(_options.TokenEncryptionKey);
            return key.Length == 32;
        }
        catch (FormatException)
        {
            key = [];
            return false;
        }
    }

    private static bool IsHttpsUrl(string value)
    {
        return Uri.TryCreate(value, UriKind.Absolute, out var uri) &&
               uri.Scheme == Uri.UriSchemeHttps;
    }

    private static string HashState(string state)
    {
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(state)));
    }

    private void EnsureConfigured()
    {
        if (!IsConfigured)
            throw new MercadoPagoApiException("A integração com o Mercado Pago ainda não está configurada.");
    }

    private sealed record CreatePaymentRequest(
        [property: JsonPropertyName("transaction_amount")] decimal TransactionAmount,
        [property: JsonPropertyName("payment_method_id")] string PaymentMethodId,
        [property: JsonPropertyName("description")] string Description,
        [property: JsonPropertyName("external_reference")] string ExternalReference,
        [property: JsonPropertyName("notification_url")] string NotificationUrl,
        [property: JsonPropertyName("date_of_expiration")] string DateOfExpiration,
        [property: JsonPropertyName("payer")] Payer Payer
    );

    private sealed record Payer(
        [property: JsonPropertyName("email")] string Email
    );

    private sealed record CreatePaymentResponse(
        [property: JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
        [property: JsonPropertyName("id")] string? Id,
        [property: JsonPropertyName("status")] string? Status,
        [property: JsonPropertyName("point_of_interaction")] PointOfInteraction? PointOfInteraction
    );

    private sealed record PointOfInteraction(
        [property: JsonPropertyName("transaction_data")] TransactionData? TransactionData
    );

    private sealed record TransactionData(
        [property: JsonPropertyName("qr_code")] string? QrCode,
        [property: JsonPropertyName("qr_code_base64")] string? QrCodeBase64
    );

    private sealed record GetPaymentResponse(
        [property: JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
        [property: JsonPropertyName("id")] string? Id,
        [property: JsonPropertyName("status")] string? Status,
        [property: JsonPropertyName("external_reference")] string? ExternalReference,
        [property: JsonPropertyName("transaction_amount")] decimal TransactionAmount,
        [property: JsonPropertyName("currency_id")] string CurrencyId,
        [property: JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
        [property: JsonPropertyName("collector_id")] string? CollectorId
    );

    private sealed record TokenResponse(
        [property: JsonPropertyName("access_token")] string? AccessToken,
        [property: JsonPropertyName("refresh_token")] string? RefreshToken,
        [property: JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
        [property: JsonPropertyName("user_id")] string? UserId,
        [property: JsonPropertyName("expires_in")] int ExpiresIn
    );
}
