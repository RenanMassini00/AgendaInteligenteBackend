using Microsoft.AspNetCore.Mvc;
using Scheduler.Api.DTOs;
using Scheduler.Api.Services.Contracts;

namespace Scheduler.Api.Controllers;

[ApiController]
[Route("api/test/whatsapp")]
public class TestWhatsAppController : ControllerBase
{
    private readonly IWhatsAppService _whatsAppService;

    public TestWhatsAppController(IWhatsAppService whatsAppService)
    {
        _whatsAppService = whatsAppService;
    }

    [HttpPost]
    public async Task<ActionResult<ApiMessage>> Send([FromQuery] string phone)
    {
        var sent = await _whatsAppService.SendTextAsync(
            phone,
            "Teste de envio WhatsApp - Massini Labs"
        );

        if (!sent)
        {
            return StatusCode(500, new ApiMessage("Falha ao enviar WhatsApp."));
        }

        return Ok(new ApiMessage("WhatsApp enviado com sucesso."));
    }
}