using Microsoft.EntityFrameworkCore;
using Scheduler.Api.Data;
using Scheduler.Api.Entities;
using Scheduler.Api.Services.Contracts;

namespace Scheduler.Api.Services;

public sealed class AppointmentDepositService(
    AppDbContext context,
    IMercadoPagoService mercadoPagoService,
    ILogger<AppointmentDepositService> logger) : IAppointmentDepositService
{
    public decimal CalculateDeposit(decimal servicePrice)
    {
        return decimal.Round(
            servicePrice * mercadoPagoService.DepositPercentage / 100m,
            2,
            MidpointRounding.AwayFromZero);
    }

    public async Task<AppointmentPayment?> CreateAsync(
        ulong accountOwnerUserId,
        Appointment appointment,
        Service service,
        string payerEmail,
        CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var payment = new AppointmentPayment
        {
            AppointmentId = appointment.Id,
            AccountOwnerUserId = accountOwnerUserId,
            PublicReference = Guid.NewGuid(),
            Status = "pending",
            Amount = CalculateDeposit(service.Price),
            ExpiresAt = now.AddMinutes(mercadoPagoService.HoldMinutes),
            CreatedAt = now,
            UpdatedAt = now
        };

        context.AppointmentPayments.Add(payment);
        await context.SaveChangesAsync(cancellationToken);

        try
        {
            var pixPayment = await mercadoPagoService.CreatePixPaymentAsync(
                accountOwnerUserId,
                appointment.Id,
                payerEmail,
                payment.Amount,
                payment.ExpiresAt,
                payment.PublicReference.ToString("N"),
                cancellationToken);

            if (string.IsNullOrWhiteSpace(pixPayment.QrCode) &&
                string.IsNullOrWhiteSpace(pixPayment.QrCodeBase64))
            {
                throw new MercadoPagoApiException("O Mercado Pago não retornou os dados do Pix.");
            }

            payment.ProviderPaymentId = pixPayment.PaymentId;
            payment.Status = "pending";
            payment.QrCode = pixPayment.QrCode;
            payment.QrCodeBase64 = pixPayment.QrCodeBase64;
            payment.UpdatedAt = DateTime.UtcNow;
            await context.SaveChangesAsync(CancellationToken.None);

            return payment;
        }
        catch (MercadoPagoApiException exception)
        {
            await MarkPaymentCreationFailedAsync(appointment, payment, cancellationToken);
            logger.LogError(exception, "Unable to create Mercado Pago payment for appointment {AppointmentId}", appointment.Id);
            return null;
        }
        catch (HttpRequestException exception)
        {
            logger.LogError(exception, "Mercado Pago was unavailable for appointment {AppointmentId}", appointment.Id);
            return null;
        }
    }

    private async Task MarkPaymentCreationFailedAsync(
        Appointment appointment,
        AppointmentPayment payment,
        CancellationToken cancellationToken)
    {
        payment.Status = "failed";
        payment.UpdatedAt = DateTime.UtcNow;
        var previousStatus = appointment.Status;
        appointment.Status = "cancelled";
        appointment.CancelledReason = "Não foi possível iniciar o pagamento do sinal.";
        appointment.UpdatedAt = DateTime.UtcNow;
        context.AppointmentStatusHistory.Add(new AppointmentStatusHistory
        {
            AppointmentId = appointment.Id,
            PreviousStatus = previousStatus,
            NewStatus = appointment.Status,
            ChangedByUserId = payment.AccountOwnerUserId,
            Note = "Reserva cancelada porque o Mercado Pago recusou a criação da cobrança.",
            CreatedAt = DateTime.UtcNow
        });
        await context.SaveChangesAsync(cancellationToken);
    }
}
