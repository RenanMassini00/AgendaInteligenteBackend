using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Scheduler.Api.Data;
using Scheduler.Api.DTOs;

namespace Scheduler.Api.Controllers;

[ApiController]
[Route("api/admin/dashboard")]
public class AdminDashboardController : ControllerBase
{
    private readonly AppDbContext _context;

    public AdminDashboardController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<AdminDashboardSummaryResponse>> Get()
    {
        var culture = CultureInfo.GetCultureInfo("pt-BR");
        var now = DateTime.Today;
        var referenceMonth = $"{now.Year}-{now.Month:00}";
        var monthStart = new DateTime(now.Year, now.Month, 1);
        var nextMonth = monthStart.AddMonths(1);

        var professionalUsers = _context.Users
            .AsNoTracking()
            .Where(x => x.Role == "professional" && x.IsActive);

        var totalCompanies = await professionalUsers.CountAsync();

        var activeCompanies = await professionalUsers.CountAsync();

        var blockedCompanies = 0;

        var receivedThisMonth = await _context.BillingRecords
            .Where(x => x.ReferenceMonth == referenceMonth && x.Status == "paid")
            .SumAsync(x => (decimal?)x.Amount) ?? 0;

        var pendingThisMonth = await _context.BillingRecords
            .Where(x => x.ReferenceMonth == referenceMonth && (x.Status == "pending" || x.Status == "overdue"))
            .SumAsync(x => (decimal?)x.Amount) ?? 0;

        var newCompaniesThisMonth = await professionalUsers
            .CountAsync(x => x.CreatedAt >= monthStart && x.CreatedAt < nextMonth);

        var professionalUserIds = await professionalUsers
            .Select(x => x.Id)
            .ToListAsync();

        var totalAppointmentsThisMonth = await _context.Appointments
            .CountAsync(x =>
                professionalUserIds.Contains(x.UserId) &&
                x.AppointmentDate >= monthStart &&
                x.AppointmentDate < nextMonth);

        var totalClients = await _context.Clients
            .CountAsync(x => professionalUserIds.Contains(x.UserId) && x.IsActive);

        var recentCompanies = await professionalUsers
            .OrderByDescending(x => x.CreatedAt)
            .Take(5)
            .Select(x => new AdminRecentCompanyResponse(
                x.Id,
                string.IsNullOrWhiteSpace(x.BusinessName) ? x.FullName : x.BusinessName!,
                x.FullName,
                "active",
                x.CreatedAt.ToString("dd/MM/yyyy")
            ))
            .ToListAsync();

        return Ok(new AdminDashboardSummaryResponse(
            totalCompanies,
            activeCompanies,
            blockedCompanies,
            receivedThisMonth,
            receivedThisMonth.ToString("C", culture),
            pendingThisMonth,
            pendingThisMonth.ToString("C", culture),
            newCompaniesThisMonth,
            totalAppointmentsThisMonth,
            totalClients,
            recentCompanies
        ));
    }
}