using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.WebUtilities;

namespace Scheduler.Api.Services;

public sealed class AuthTokenService
{
    private readonly byte[] _signingKey;

    public AuthTokenService(IConfiguration configuration)
    {
        var key = configuration["Authentication:SigningKey"];
        if (string.IsNullOrWhiteSpace(key) || Encoding.UTF8.GetByteCount(key) < 32)
            throw new InvalidOperationException(
                "A variável Authentication__SigningKey é obrigatória e deve conter pelo menos 32 bytes.");

        _signingKey = Encoding.UTF8.GetBytes(key);
    }

    public string CreateToken(ulong userId, string role)
    {
        var expiresAt = DateTimeOffset.UtcNow.AddHours(12).ToUnixTimeSeconds();
        var payload = $"{userId}|{role}|{expiresAt}";
        var payloadBytes = Encoding.UTF8.GetBytes(payload);
        var signature = HMACSHA256.HashData(_signingKey, payloadBytes);
        return $"{WebEncoders.Base64UrlEncode(payloadBytes)}.{WebEncoders.Base64UrlEncode(signature)}";
    }

    public ClaimsPrincipal? ValidateToken(string token)
    {
        if (token.Length > 4096)
            return null;

        var parts = token.Split('.');
        if (parts.Length != 2)
            return null;

        byte[] payloadBytes;
        byte[] suppliedSignature;
        try
        {
            payloadBytes = WebEncoders.Base64UrlDecode(parts[0]);
            suppliedSignature = WebEncoders.Base64UrlDecode(parts[1]);
        }
        catch (FormatException)
        {
            return null;
        }

        var expectedSignature = HMACSHA256.HashData(_signingKey, payloadBytes);
        if (suppliedSignature.Length != expectedSignature.Length ||
            !CryptographicOperations.FixedTimeEquals(suppliedSignature, expectedSignature))
            return null;

        var payload = Encoding.UTF8.GetString(payloadBytes).Split('|');
        if (payload.Length != 3 ||
            !ulong.TryParse(payload[0], out var userId) ||
            userId == 0 ||
            !long.TryParse(payload[2], out var expiresAt) ||
            expiresAt <= DateTimeOffset.UtcNow.ToUnixTimeSeconds() ||
            payload[1] is not ("professional" or "employee" or "client" or "master_admin"))
            return null;

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(ClaimTypes.Role, payload[1])
        };
        return new ClaimsPrincipal(new ClaimsIdentity(claims, "Bearer"));
    }

    public static string NormalizeRoleForToken(string role)
    {
        var normalized = role.Trim().ToLowerInvariant();
        return normalized switch
        {
            "master admin" or "master_admin" => "master_admin",
            "professional" => "professional",
            "employee" => "employee",
            "client" => "client",
            _ => throw new InvalidOperationException($"Papel de usuário não suportado: {role}")
        };
    }
}
