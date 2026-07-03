using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Scheduler.Api.Data;
using Scheduler.Api.DTOs;

namespace Scheduler.Api.Controllers;

[ApiController]
[Route("api/admin/appointments")]
public class AdminAppointmentsController : ControllerBase
{
    private readonly AppDbContext _context;

    public AdminAppointmentsController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    [HttpGet("counts")]
    public async Task<ActionResult<AdminAppointmentCountsResponse>> GetCounts(
        [FromQuery] string? date = null,
        [FromQuery] string? month = null)
    {
        var period = BuildPeriod(date, month);
        if (!string.IsNullOrWhiteSpace(period.ErrorMessage))
        {
            return BadRequest(new ApiMessage(period.ErrorMessage));
        }

        var companyIds = await _context.Users
            .AsNoTracking()
            .Where(x => x.Role == "professional" && x.IsActive)
            .OrderBy(x => x.BusinessName)
            .ThenBy(x => x.FullName)
            .Select(x => x.Id)
            .ToListAsync();

        var appointmentsQuery = _context.Appointments
            .AsNoTracking()
            .Where(x => companyIds.Contains(x.UserId));

        if (period.Start.HasValue && period.End.HasValue)
        {
            appointmentsQuery = appointmentsQuery.Where(x =>
                x.AppointmentDate >= period.Start.Value &&
                x.AppointmentDate < period.End.Value);
        }

        var groupedCounts = await appointmentsQuery
            .GroupBy(x => x.UserId)
            .Select(x => new
            {
                Id = x.Key,
                AppointmentsCount = x.Count()
            })
            .ToListAsync();

        var countsByCompanyId = groupedCounts.ToDictionary(x => x.Id, x => x.AppointmentsCount);

        var items = companyIds
            .Select(id => new AdminAppointmentCountResponse(
                id,
                countsByCompanyId.GetValueOrDefault(id)))
            .ToList();

        return Ok(new AdminAppointmentCountsResponse(
            period.PeriodType,
            period.Date,
            period.Month,
            items.Sum(x => x.AppointmentsCount),
            items
        ));
    }

    private static AppointmentCountPeriod BuildPeriod(string? date, string? month)
    {
        if (!string.IsNullOrWhiteSpace(date))
        {
            if (!DateTime.TryParseExact(
                    date,
                    "yyyy-MM-dd",
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None,
                    out var parsedDate))
            {
                return AppointmentCountPeriod.Invalid("Data inválida. Use o formato yyyy-MM-dd.");
            }

            var start = parsedDate.Date;
            return new AppointmentCountPeriod(
                "day",
                start,
                start.AddDays(1),
                start.ToString("yyyy-MM-dd"),
                null,
                null);
        }

        if (!string.IsNullOrWhiteSpace(month))
        {
            if (!DateTime.TryParseExact(
                    month + "-01",
                    "yyyy-MM-dd",
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None,
                    out var parsedMonth))
            {
                return AppointmentCountPeriod.Invalid("Mês inválido. Use o formato yyyy-MM.");
            }

            var start = new DateTime(parsedMonth.Year, parsedMonth.Month, 1);
            return new AppointmentCountPeriod(
                "month",
                start,
                start.AddMonths(1),
                null,
                start.ToString("yyyy-MM"),
                null);
        }

        return new AppointmentCountPeriod("all", null, null, null, null, null);
    }

    private sealed record AppointmentCountPeriod(
        string PeriodType,
        DateTime? Start,
        DateTime? End,
        string? Date,
        string? Month,
        string? ErrorMessage)
    {
        public static AppointmentCountPeriod Invalid(string message) =>
            new("invalid", null, null, null, null, message);
    }
}
