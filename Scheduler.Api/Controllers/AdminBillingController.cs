using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Scheduler.Api.Data;
using Scheduler.Api.DTOs;
using Scheduler.Api.Entities;

namespace Scheduler.Api.Controllers;

[ApiController]
[Route("api/admin/billing")]
public class AdminBillingController : ControllerBase
{
    private readonly AppDbContext _context;

    public AdminBillingController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<List<AdminBillingResponse>>> GetAll([FromQuery] string? month = null)
    {
        var culture = CultureInfo.GetCultureInfo("pt-BR");

        var query = _context.BillingRecords
            .AsNoTracking()
            .Include(x => x.Company)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(month))
        {
            query = query.Where(x => x.ReferenceMonth == month);
        }

        var items = await query
            .OrderByDescending(x => x.ReferenceMonth)
            .ThenBy(x => x.DueDate)
            .ToListAsync();

        return Ok(items.Select(x => new AdminBillingResponse(
            x.Id,
            x.CompanyId,
            x.Company != null ? x.Company.Name : "Empresa",
            x.ReferenceMonth,
            x.Amount,
            x.Amount.ToString("C", culture),
            x.DueDate.ToString("dd/MM/yyyy"),
            x.PaidAt.HasValue ? x.PaidAt.Value.ToString("dd/MM/yyyy HH:mm") : null,
            x.Status,
            x.PaymentMethod,
            x.Notes
        )).ToList());
    }
}