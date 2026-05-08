using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Scheduler.Api.Data;
using Scheduler.Api.DTOs;

namespace Scheduler.Api.Controllers;

[ApiController]
[Route("api/admin/companies")]
public class AdminCompaniesController : ControllerBase
{
    private readonly AppDbContext _context;

    public AdminCompaniesController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<List<AdminCompanyResponse>>> GetAll()
    {
        var culture = CultureInfo.GetCultureInfo("pt-BR");

        var users = await _context.Users
            .AsNoTracking()
            .Where(x => x.Role == "professional" && x.IsActive)
            .OrderBy(x => x.BusinessName)
            .ThenBy(x => x.FullName)
            .ToListAsync();

        var result = new List<AdminCompanyResponse>();

        foreach (var user in users)
        {
            var clientsCount = await _context.Clients
                .CountAsync(x => x.UserId == user.Id && x.IsActive);

            var servicesCount = await _context.Services
                .CountAsync(x => x.UserId == user.Id && x.IsActive);

            var appointmentsCount = await _context.Appointments
                .CountAsync(x => x.UserId == user.Id);

            var latestBilling = await _context.BillingRecords
                .Where(x => x.CompanyId == user.Id)
                .OrderByDescending(x => x.ReferenceMonth)
                .FirstOrDefaultAsync();

            var monthlyFee = latestBilling?.Amount ?? 0;

            result.Add(new AdminCompanyResponse(
                user.Id,
                string.IsNullOrWhiteSpace(user.BusinessName) ? user.FullName : user.BusinessName!,
                user.FullName,
                user.Email,
                user.Phone,
                null,
                null,
                user.PublicSlug,
                user.IsActive ? "active" : "inactive",
                monthlyFee,
                monthlyFee.ToString("C", culture),
                null,
                user.CreatedAt.ToString("dd/MM/yyyy"),
                1,
                clientsCount,
                servicesCount,
                appointmentsCount
            ));
        }

        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<AdminCompanyResponse>> GetById(ulong id)
    {
        var culture = CultureInfo.GetCultureInfo("pt-BR");

        var user = await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id && x.Role == "professional");

        if (user is null)
        {
            return NotFound(new ApiMessage("Empresa não encontrada."));
        }

        var clientsCount = await _context.Clients
            .CountAsync(x => x.UserId == user.Id && x.IsActive);

        var servicesCount = await _context.Services
            .CountAsync(x => x.UserId == user.Id && x.IsActive);

        var appointmentsCount = await _context.Appointments
            .CountAsync(x => x.UserId == user.Id);

        var latestBilling = await _context.BillingRecords
            .Where(x => x.CompanyId == user.Id)
            .OrderByDescending(x => x.ReferenceMonth)
            .FirstOrDefaultAsync();

        var monthlyFee = latestBilling?.Amount ?? 0;

        return Ok(new AdminCompanyResponse(
            user.Id,
            string.IsNullOrWhiteSpace(user.BusinessName) ? user.FullName : user.BusinessName!,
            user.FullName,
            user.Email,
            user.Phone,
            null,
            null,
            user.PublicSlug,
            user.IsActive ? "active" : "inactive",
            monthlyFee,
            monthlyFee.ToString("C", culture),
            null,
            user.CreatedAt.ToString("dd/MM/yyyy"),
            1,
            clientsCount,
            servicesCount,
            appointmentsCount
        ));
    }
}