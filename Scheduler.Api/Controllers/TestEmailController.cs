using Microsoft.AspNetCore.Mvc;
using Scheduler.Api.DTOs;
using Scheduler.Api.Services.Contracts;

namespace Scheduler.Api.Controllers;

[ApiController]
[Route("api/test/email")]
public class TestEmailController : ControllerBase
{
    private readonly IEmailService _emailService;

    public TestEmailController(IEmailService emailService)
    {
        _emailService = emailService;
    }

    [HttpPost]
    public async Task<ActionResult<ApiMessage>> Send([FromQuery] string to)
    {
        var html =
            """
            <div style="font-family: Arial, sans-serif">
              <h2>Teste de e-mail</h2>
              <p>Se você recebeu esta mensagem, o SMTP está funcionando.</p>
            </div>
            """;

        var sent = await _emailService.SendAsync(to, "Teste de e-mail - Massini Labs", html);

        if (!sent)
        {
            return StatusCode(500, new ApiMessage("Falha ao enviar o e-mail de teste. Veja o log da API."));
        }

        return Ok(new ApiMessage("E-mail de teste enviado com sucesso."));
    }
}