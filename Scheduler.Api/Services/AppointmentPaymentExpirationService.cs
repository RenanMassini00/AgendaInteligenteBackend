using Microsoft.EntityFrameworkCore;
using Scheduler.Api.Data;
using Scheduler.Api.Entities;

namespace Scheduler.Api.Services;

public sealed class AppointmentPaymentExpirationService(
    IServiceScopeFactory scopeFactory,
    ILogger<AppointmentPaymentExpirationService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(30));

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var now = DateTime.UtcNow;

            var expired = await (
                from payment in context.AppointmentPayments
                join appointment in context.Appointments on payment.AppointmentId equals appointment.Id
                where payment.Status == "pending" &&
                      payment.ExpiresAt <= now
                select new { Payment = payment, Appointment = appointment }
            ).ToListAsync(stoppingToken);

            if (expired.Count == 0)
                continue;

            foreach (var item in expired)
            {
                var previousStatus = item.Appointment.Status;
                item.Payment.Status = "expired";
                item.Payment.UpdatedAt = now;
                if (item.Appointment.Status != "pending_payment")
                    continue;

                item.Appointment.Status = "cancelled";
                item.Appointment.CancelledReason = "Prazo para pagamento do sinal expirado.";
                item.Appointment.UpdatedAt = now;
                context.AppointmentStatusHistory.Add(new AppointmentStatusHistory
                {
                    AppointmentId = item.Appointment.Id,
                    PreviousStatus = previousStatus,
                    NewStatus = item.Appointment.Status,
                    ChangedByUserId = item.Appointment.UserId,
                    Note = "Reserva cancelada pelo sistema após expirar o prazo do pagamento.",
                    CreatedAt = now
                });
            }

            await context.SaveChangesAsync(stoppingToken);
            logger.LogInformation(
                "Expired {PaymentCount} unpaid public appointment reservations",
                expired.Count);
        }
    }
}
