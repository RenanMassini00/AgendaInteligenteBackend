namespace Scheduler.Api.DTOs;

public record AdminDashboardSummaryResponse(
    int TotalCompanies,
    int ActiveCompanies,
    int BlockedCompanies,
    decimal ReceivedThisMonth,
    string ReceivedThisMonthFormatted,
    decimal PendingThisMonth,
    string PendingThisMonthFormatted,
    int NewCompaniesThisMonth,
    int TotalAppointmentsThisMonth,
    int TotalClients,
    List<AdminRecentCompanyResponse> RecentCompanies
);

public record AdminRecentCompanyResponse(
    ulong Id,
    string Name,
    string OwnerName,
    string Status,
    string CreatedAt
);

public record AdminCompanyResponse(
    ulong Id,
    string Name,
    string OwnerName,
    string Email,
    string? Phone,
    string? Document,
    string? LogoUrl,
    string? PublicSlug,
    string Status,
    decimal MonthlyFee,
    string MonthlyFeeFormatted,
    string? Notes,
    string CreatedAt,
    int ProfessionalsCount,
    int ClientsCount,
    int ServicesCount,
    int AppointmentsCount
);

public record AdminBillingResponse(
    ulong Id,
    ulong CompanyId,
    string CompanyName,
    string ReferenceMonth,
    decimal Amount,
    string AmountFormatted,
    string DueDate,
    string? PaidAt,
    string Status,
    string? PaymentMethod,
    string? Notes
);

public record AdminUserResponse(
    ulong Id,
    string FullName,
    string? BusinessName,
    string Email,
    string? Phone,
    string? Specialty,
    string Role,
    string Status,
    string CreatedAt,
    string? PublicSlug,
    string? Timezone
);

public record AdminUserCreateRequest(
    string FullName,
    string? BusinessName,
    string Email,
    string? Phone,
    string? Specialty,
    string Password,
    string? PublicSlug,
    string? Timezone
);

public record AdminUserUpdateRequest(
    string FullName,
    string? BusinessName,
    string Email,
    string? Phone,
    string? Specialty,
    string? Password,
    string? PublicSlug,
    string? Timezone,
    bool IsActive
);