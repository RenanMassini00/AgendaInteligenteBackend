using Scheduler.Api.Entities;

namespace Scheduler.Api.Services.Contracts;

public interface IAppointmentDepositService
{
    decimal CalculateDeposit(decimal servicePrice);

    Task<AppointmentPayment?> CreateAsync(
        ulong accountOwnerUserId,
        Appointment appointment,
        Service service,
        string payerEmail,
        CancellationToken cancellationToken = default);
}
