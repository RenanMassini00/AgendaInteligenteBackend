namespace Scheduler.Api.Entities;

public class BillingRecord
{
    public ulong Id { get; set; }
    public ulong CompanyId { get; set; }
    public string ReferenceMonth { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public DateTime DueDate { get; set; }
    public DateTime? PaidAt { get; set; }
    public string Status { get; set; } = "pending";
    public string? PaymentMethod { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public Company? Company { get; set; }
}