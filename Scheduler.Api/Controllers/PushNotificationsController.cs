using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Scheduler.Api.DTOs;
using Scheduler.Api.Options;
using Scheduler.Api.Services.Contracts;

namespace Scheduler.Api.Controllers;

[ApiController]
[Route("api/push")]
public class PushNotificationsController : ControllerBase
{
    private readonly WebPushOptions _options;
    private readonly IPushNotificationService _pushNotificationService;

    public PushNotificationsController(
        IOptions<WebPushOptions> options,
        IPushNotificationService pushNotificationService)
    {
        _options = options.Value;
        _pushNotificationService = pushNotificationService;
    }

    [HttpGet("public-key")]
    public ActionResult<PushPublicKeyResponse> GetPublicKey()
    {
        var enabled = _options.Enabled && !string.IsNullOrWhiteSpace(_options.PublicKey);
        return Ok(new PushPublicKeyResponse(
            enabled,
            enabled ? _options.PublicKey : null
        ));
    }

    [HttpPost("subscriptions")]
    public async Task<ActionResult<ApiMessage>> Register(
        [FromQuery] ulong userId,
        [FromBody] PushSubscriptionRegisterRequest request)
    {
        if (userId == 0)
        {
            return BadRequest(new ApiMessage("Usuário inválido."));
        }

        var registered = await _pushNotificationService.RegisterSubscriptionAsync(
            userId,
            request,
            HttpContext.RequestAborted);

        if (!registered)
        {
            return BadRequest(new ApiMessage("Não foi possível registrar este dispositivo."));
        }

        return Ok(new ApiMessage("Dispositivo registrado para notificações."));
    }

    [HttpDelete("subscriptions")]
    public async Task<ActionResult<ApiMessage>> Remove(
        [FromQuery] ulong userId,
        [FromBody] PushSubscriptionRemoveRequest request)
    {
        if (userId == 0)
        {
            return BadRequest(new ApiMessage("Usuário inválido."));
        }

        var removed = await _pushNotificationService.RemoveSubscriptionAsync(
            userId,
            request.Endpoint,
            HttpContext.RequestAborted);

        if (!removed)
        {
            return NotFound(new ApiMessage("Assinatura de notificação não encontrada."));
        }

        return Ok(new ApiMessage("Dispositivo removido das notificações."));
    }

    [HttpPost("test")]
    public async Task<ActionResult<ApiMessage>> Test(
        [FromQuery] ulong userId,
        [FromBody] PushTestRequest request)
    {
        if (userId == 0)
        {
            return BadRequest(new ApiMessage("Usuário inválido."));
        }

        var sent = await _pushNotificationService.SendToUserAsync(
            userId,
            string.IsNullOrWhiteSpace(request.Title) ? "Notificação ativada" : request.Title.Trim(),
            string.IsNullOrWhiteSpace(request.Body) ? "Seu app já pode receber notificações." : request.Body.Trim(),
            request.Url,
            "push-test",
            null,
            HttpContext.RequestAborted);

        if (!sent)
        {
            return StatusCode(500, new ApiMessage("Nenhum dispositivo ativo recebeu a notificação."));
        }

        return Ok(new ApiMessage("Notificação enviada com sucesso."));
    }
}
