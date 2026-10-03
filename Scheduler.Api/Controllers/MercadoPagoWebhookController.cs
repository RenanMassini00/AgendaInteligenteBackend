using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Scheduler.Api.Data;
using Scheduler.Api.DTOs;
using Scheduler.Api.Entities;
using Scheduler.Api.Options;
using Scheduler.Api.Services.Contracts;

namespace Scheduler.Api.Controllers;

[ApiController]
[Route("api/payments/mercadopago")]
public sealed class MercadoPagoWebhookController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IMercadoPagoService _mercadoPagoService;
    private readonly IBookingAutomationService _bookingAutomationService;
    private readonly MercadoPagoOptions _options;
    private readonly ILogger<MercadoPagoWebhookController> _logger;

    public MercadoPagoWebhookController(
        AppDbContext context,
        IMercadoPagoService mercadoPagoService,
        IBookingAutomationService bookingAutomationService,
        IOptions<MercadoPagoOptions> options,
        ILogger<MercadoPagoWebhookController> logger)
    {
        _context = context;
        _mercadoPagoService = mercadoPagoService;
        _bookingAutomationService = bookingAutomationService;
        _options = options.Value;
        _logger = logger;
    }

    [HttpPost("webhook")]
    public async Task<IActionResult> Webhook(
        [FromQuery(Name = "data.id")] string? paymentId,
        [FromQuery] string? type,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(paymentId) ||
            !string.Equals(type, "payment", StringComparison.OrdinalIgnoreCase))
        {
            return Ok();
        }

        if (!IsValidSignature(paymentId))
        {
            _logger.LogWarning("Rejected Mercado Pago webhook with an invalid signature");
            return Unauthorized();
        }

        var payment = await _context.AppointmentPayments
            .FirstOrDefaultAsync(x => x.ProviderPaymentId == paymentId, cancellationToken);

        if (payment is null)
        {
            _logger.LogWarning("No local payment found for Mercado Pago payment {PaymentId}", paymentId);
            return StatusCode(StatusCodes.Status503ServiceUnavailable);
        }

        var providerPayment = await _mercadoPagoService.GetPaymentAsync(
            payment.AccountOwnerUserId,
            paymentId,
            cancellationToken);

        if (!ulong.TryParse(providerPayment.ExternalReference, out var appointmentId) ||
            appointmentId != payment.AppointmentId)
        {
            _logger.LogWarning("Mercado Pago payment {PaymentId} has an invalid appointment reference", paymentId);
            return Ok();
        }

        var connectedAccount = await _context.MercadoPagoAccounts
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.UserId == payment.AccountOwnerUserId, cancellationToken);
        if (connectedAccount is null ||
            connectedAccount.MercadoPagoUserId != providerPayment.CollectorId)
        {
            _logger.LogError("Mercado Pago collector mismatch for appointment {AppointmentId}", appointmentId);
            return Ok();
        }

        if (providerPayment.Currency != "BRL" ||
            providerPayment.Amount != payment.Amount)
        {
            _logger.LogError("Mercado Pago amount or currency mismatch for appointment {AppointmentId}", appointmentId);
            return Ok();
        }

        var appointment = await _context.Appointments
            .FirstOrDefaultAsync(x => x.Id == appointmentId, cancellationToken);
        if (appointment is null)
            return Ok();

        payment.ProviderPaymentId = providerPayment.PaymentId;
        payment.Status = NormalizeStatus(providerPayment.Status);
        payment.UpdatedAt = DateTime.UtcNow;

        if (providerPayment.Status == "approved" &&
            payment.ExpiresAt > DateTime.UtcNow &&
            appointment.Status == "pending_payment")
        {
            var previousStatus = appointment.Status;
            appointment.Status = "confirmed";
            appointment.UpdatedAt = DateTime.UtcNow;
            _context.AppointmentStatusHistory.Add(new AppointmentStatusHistory
            {
                AppointmentId = appointment.Id,
                PreviousStatus = previousStatus,
                NewStatus = appointment.Status,
                ChangedByUserId = appointment.UserId,
                Note = "Sinal confirmado pelo Mercado Pago.",
                CreatedAt = DateTime.UtcNow
            });

            await _context.SaveChangesAsync(cancellationToken);

            var client = await _context.Clients
                .FirstOrDefaultAsync(x => x.Id == appointment.ClientId, cancellationToken);
            var service = await _context.Services
                .FirstOrDefaultAsync(x => x.Id == appointment.ServiceId, cancellationToken);
            var professional = await _context.Users
                .FirstOrDefaultAsync(x => x.Id == appointment.UserId, cancellationToken);

            if (client is null || service is null || professional is null)
            {
                _logger.LogError("Confirmed payment {PaymentId} but appointment {AppointmentId} relations are incomplete",
                    payment.ProviderPaymentId, appointment.Id);
                return Ok();
            }

            var userSetting = await _context.UserSettings
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.UserId == professional.Id, cancellationToken);

            await _bookingAutomationService.ProcessAsync(
                professional,
                userSetting,
                client,
                service,
                appointment);
        }
        else
        {
            if (providerPayment.Status == "approved" && appointment.Status != "confirmed")
            {
                payment.Status = "approved_after_expiry";
                _logger.LogWarning(
                    "Payment {PaymentId} was approved after appointment {AppointmentId} was no longer pending",
                    payment.ProviderPaymentId,
                    appointment.Id);
            }

            await _context.SaveChangesAsync(cancellationToken);
        }

        return Ok();
    }

    [HttpGet("~/api/public/payments/{reference:guid}")]
    public async Task<ActionResult<PublicPaymentStatusResponse>> GetStatus(
        Guid reference,
        CancellationToken cancellationToken)
    {
        var payment = await _context.AppointmentPayments
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.PublicReference == reference, cancellationToken);

        if (payment is null)
            return NotFound(new ApiMessage("Pagamento não encontrado."));

        var appointmentStatus = await _context.Appointments
            .AsNoTracking()
            .Where(x => x.Id == payment.AppointmentId)
            .Select(x => x.Status)
            .FirstOrDefaultAsync(cancellationToken);

        if (appointmentStatus is null)
            return NotFound(new ApiMessage("Agendamento não encontrado."));

        return Ok(new PublicPaymentStatusResponse(
            payment.Status,
            appointmentStatus,
            payment.Amount,
            DateTime.SpecifyKind(payment.ExpiresAt, DateTimeKind.Utc)));
    }

    private bool IsValidSignature(string paymentId)
    {
        var signatureHeader = Request.Headers["x-signature"].ToString();
        var requestId = Request.Headers["x-request-id"].ToString();
        if (string.IsNullOrWhiteSpace(_options.WebhookSecret) ||
            string.IsNullOrWhiteSpace(signatureHeader) ||
            string.IsNullOrWhiteSpace(requestId))
        {
            return false;
        }

        var signatureParts = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var part in signatureHeader.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var pair = part.Split('=', 2);
            if (pair.Length != 2 || !signatureParts.TryAdd(pair[0], pair[1]))
                return false;
        }

        if (!signatureParts.TryGetValue("ts", out var timestamp) ||
            !signatureParts.TryGetValue("v1", out var signature))
        {
            return false;
        }

        var manifest = $"id:{paymentId.ToLowerInvariant()};request-id:{requestId};ts:{timestamp};";
        var expected = HMACSHA256.HashData(
            Encoding.UTF8.GetBytes(_options.WebhookSecret),
            Encoding.UTF8.GetBytes(manifest));

        try
        {
            var received = Convert.FromHexString(signature);
            return CryptographicOperations.FixedTimeEquals(expected, received);
        }
        catch (FormatException)
        {
            return false;
        }
    }

    private static string NormalizeStatus(string status)
    {
        return status switch
        {
            "approved" => "approved",
            "rejected" => "rejected",
            "cancelled" => "cancelled",
            "refunded" => "refunded",
            "charged_back" => "charged_back",
            _ => "pending"
        };
    }
}
