namespace Scheduler.Api.Services.Contracts;

public interface IMercadoPagoService
{
    bool IsConfigured { get; }
    decimal DepositPercentage { get; }
    int HoldMinutes { get; }
    Task<string> CreateAuthorizationUrlAsync(ulong accountOwnerUserId, CancellationToken cancellationToken = default);
    Task CompleteAuthorizationAsync(string code, string state, CancellationToken cancellationToken = default);
    Task<bool> HasConnectedAccountAsync(ulong accountOwnerUserId, CancellationToken cancellationToken = default);

    Task<MercadoPagoPixPayment> CreatePixPaymentAsync(
        ulong accountOwnerUserId,
        ulong appointmentId,
        string payerEmail,
        decimal amount,
        DateTime expiresAt,
        string idempotencyKey,
        CancellationToken cancellationToken = default);

    Task<MercadoPagoPaymentDetails> GetPaymentAsync(
        ulong accountOwnerUserId,
        string paymentId,
        CancellationToken cancellationToken = default);
}

public sealed record MercadoPagoPixPayment(
    string PaymentId,
    string Status,
    string? QrCode,
    string? QrCodeBase64
);

public sealed record MercadoPagoPaymentDetails(
    string PaymentId,
    string Status,
    string ExternalReference,
    decimal Amount,
    string Currency,
    string CollectorId
);

public sealed class MercadoPagoApiException(string message) : Exception(message)
{
}
