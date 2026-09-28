using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using Scheduler.Api.Services;

namespace Scheduler.Api.Services.Authentication;

public sealed class BearerTokenHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    private readonly AuthTokenService _authTokenService;

    public BearerTokenHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        AuthTokenService authTokenService)
        : base(options, logger, encoder)
    {
        _authTokenService = authTokenService;
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var authorization = Request.Headers["Authorization"].ToString();
        if (string.IsNullOrWhiteSpace(authorization))
            return Task.FromResult(AuthenticateResult.NoResult());

        if (!authorization.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            return Task.FromResult(AuthenticateResult.Fail("Esquema de autenticação inválido."));

        var token = authorization["Bearer ".Length..].Trim();
        var principal = _authTokenService.ValidateToken(token);
        if (principal is null)
            return Task.FromResult(AuthenticateResult.Fail("Token inválido ou expirado."));

        var ticket = new AuthenticationTicket(principal, Scheme.Name);
        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
