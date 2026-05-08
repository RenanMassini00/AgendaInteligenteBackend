namespace Scheduler.Api.Entities;

public class Company
{
    public ulong Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string OwnerName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Document { get; set; }
    public string? LogoUrl { get; set; }
    public string? PublicSlug { get; set; }
    public string Status { get; set; } = "active";
    public decimal MonthlyFee { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public ICollection<User> Users { get; set; } = new List<User>();
    public ICollection<BillingRecord> BillingRecords { get; set; } = new List<BillingRecord>();
}