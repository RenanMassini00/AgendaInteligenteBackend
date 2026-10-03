using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Scheduler.Api.DTOs;
using Scheduler.Api.Services;
using Scheduler.Api.Services.Contracts;

namespace Scheduler.Api.Controllers;

[ApiController]
[Route("api/mercadopago")]
public sealed class MercadoPagoOAuthController : ControllerBase
{
    private readonly IMercadoPagoService _mercadoPagoService;
    private readonly AuthenticatedUserScope _userScope;

    public MercadoPagoOAuthController(
        IMercadoPagoService mercadoPagoService,
        AuthenticatedUserScope userScope)
    {
        _mercadoPagoService = mercadoPagoService;
        _userScope = userScope;
    }

    [Authorize(Roles = "professional")]
    [HttpGet("connection")]
    public async Task<IActionResult> GetConnection(CancellationToken cancellationToken)
    {
        var user = await _userScope.GetCurrentUserAsync(User);
        if (user is null)
            return Unauthorized(new ApiMessage("Usuário não encontrado ou inativo."));

        var owner = await _userScope.GetBusinessOwnerAsync(user);
        if (owner is null)
            return Forbid();

        return Ok(new { connected = await _mercadoPagoService.HasConnectedAccountAsync(owner.Id, cancellationToken) });
    }

    [Authorize(Roles = "professional")]
    [HttpPost("connect")]
    public async Task<IActionResult> Connect(CancellationToken cancellationToken)
    {
        var user = await _userScope.GetCurrentUserAsync(User);
        if (user is null)
            return Unauthorized(new ApiMessage("Usuário não encontrado ou inativo."));

        var owner = await _userScope.GetBusinessOwnerAsync(user);
        if (owner is null)
            return Forbid();

        try
        {
            var authorizationUrl = await _mercadoPagoService.CreateAuthorizationUrlAsync(owner.Id, cancellationToken);
            return Ok(new { authorizationUrl });
        }
        catch (MercadoPagoApiException)
        {
            return StatusCode(
                StatusCodes.Status503ServiceUnavailable,
                new ApiMessage("A integração do Mercado Pago ainda não está configurada."));
        }
    }

    [AllowAnonymous]
    [HttpGet("oauth/callback")]
    public async Task<IActionResult> Callback(
        [FromQuery] string? code,
        [FromQuery] string? state,
        [FromQuery] string? error,
        CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(error))
            return BadRequest(new ApiMessage("A conexão com o Mercado Pago foi cancelada."));

        if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(state))
            return BadRequest(new ApiMessage("Retorno de autorização do Mercado Pago incompleto."));

        try
        {
            await _mercadoPagoService.CompleteAuthorizationAsync(code, state, cancellationToken);
            return Ok(new { connected = true, message = "Conta Mercado Pago conectada com sucesso." });
        }
        catch (MercadoPagoApiException exception)
        {
            return BadRequest(new ApiMessage(exception.Message));
        }
    }
}
