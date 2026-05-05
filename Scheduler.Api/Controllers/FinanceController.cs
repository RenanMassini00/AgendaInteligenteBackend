using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Scheduler.Api.Data;
using Scheduler.Api.DTOs;

namespace Scheduler.Api.Controllers;

[ApiController]
[Route("api/finance")]
public class FinanceController : ControllerBase
{
    private readonly AppDbContext _context;

    public FinanceController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet("summary")]
    public async Task<ActionResult<FinanceSummaryResponse>> GetSummary(
        [FromQuery] ulong userId = 1,
        [FromQuery] string? month = null)
    {
        var culture = CultureInfo.GetCultureInfo("pt-BR");

        DateTime targetMonth;
        if (!string.IsNullOrWhiteSpace(month) &&
            DateTime.TryParseExact(month + "-01", "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsedMonth))
        {
            targetMonth = parsedMonth;
        }
        else
        {
            var now = DateTime.Today;
            targetMonth = new DateTime(now.Year, now.Month, 1);
        }

        var startDate = new DateTime(targetMonth.Year, targetMonth.Month, 1);
        var endDate = startDate.AddMonths(1).AddDays(-1);

        var appointments = await _context.Appointments
            .AsNoTracking()
            .Include(x => x.Client)
            .Include(x => x.Service)
            .Where(x =>
                x.UserId == userId &&
                x.AppointmentDate >= startDate &&
                x.AppointmentDate <= endDate)
            .OrderBy(x => x.AppointmentDate)
            .ThenBy(x => x.StartTime)
            .ToListAsync();

        var receivedStatuses = new[] { "completed", "concluded", "done" };
        var forecastStatuses = new[] { "scheduled", "confirmed", "completed", "concluded", "done" };

        var receivedAppointments = appointments
            .Where(x => receivedStatuses.Contains((x.Status ?? string.Empty).ToLower()))
            .ToList();

        var forecastAppointments = appointments
            .Where(x => forecastStatuses.Contains((x.Status ?? string.Empty).ToLower()))
            .ToList();

        var receivedTotal = receivedAppointments.Sum(x => x.PriceAtBooking);
        var forecastTotal = forecastAppointments.Sum(x => x.PriceAtBooking);
        var averageTicket = receivedAppointments.Count == 0
            ? 0
            : receivedAppointments.Average(x => x.PriceAtBooking);

        var dailyTotals = receivedAppointments
            .GroupBy(x => x.AppointmentDate.Date)
            .OrderBy(x => x.Key)
            .Select(group => new FinanceDailyItemResponse(
                group.Key.ToString("dd/MM/yyyy"),
                group.Sum(x => x.PriceAtBooking),
                group.Sum(x => x.PriceAtBooking).ToString("C", culture),
                group.Count()
            ))
            .ToList();

        var bestDay = dailyTotals
            .OrderByDescending(x => x.Amount)
            .FirstOrDefault();

        var groupedStatus = appointments
            .GroupBy(x => (x.Status ?? string.Empty).ToLower())
            .Select(group => new FinanceStatusTotalResponse(
                group.Key,
                GetStatusLabel(group.Key),
                group.Count(),
                group.Sum(x => x.PriceAtBooking),
                group.Sum(x => x.PriceAtBooking).ToString("C", culture)
            ))
            .OrderByDescending(x => x.Amount)
            .ToList();

        var serviceTotals = receivedAppointments
            .GroupBy(x => x.Service != null ? x.Service.Name : "Serviço")
            .Select(group => new FinanceServiceTotalResponse(
                group.Key,
                group.Count(),
                group.Sum(x => x.PriceAtBooking),
                group.Sum(x => x.PriceAtBooking).ToString("C", culture)
            ))
            .OrderByDescending(x => x.Amount)
            .ToList();

        var topServiceName = serviceTotals.FirstOrDefault()?.ServiceName;

        var completionRate = appointments.Count == 0
            ? 0
            : (decimal)receivedAppointments.Count / appointments.Count * 100m;

        var appointmentItems = appointments
            .Select(x => new FinanceAppointmentItemResponse(
                x.Id,
                x.Client != null ? x.Client.FullName : "Cliente",
                x.Service != null ? x.Service.Name : "Serviço",
                x.AppointmentDate.ToString("dd/MM/yyyy"),
                x.StartTime.ToString(@"hh\:mm"),
                GetStatusLabel(x.Status),
                x.PriceAtBooking,
                x.PriceAtBooking.ToString("C", culture)
            ))
            .ToList();

        var response = new FinanceSummaryResponse(
            startDate.ToString("yyyy-MM"),
            startDate.ToString("MMMM 'de' yyyy", culture),
            receivedTotal,
            receivedTotal.ToString("C", culture),
            forecastTotal,
            forecastTotal.ToString("C", culture),
            appointments.Count,
            receivedAppointments.Count,
            averageTicket,
            averageTicket.ToString("C", culture),
            bestDay?.Date,
            bestDay?.Amount ?? 0,
            (bestDay?.Amount ?? 0).ToString("C", culture),
            completionRate,
            $"{completionRate:0.##}%",
            topServiceName,
            dailyTotals,
            groupedStatus,
            serviceTotals,
            appointmentItems
        );

        return Ok(response);
    }

    private static string GetStatusLabel(string? status)
    {
        return (status ?? string.Empty).ToLower() switch
        {
            "scheduled" => "Agendado",
            "confirmed" => "Confirmado",
            "completed" => "Concluído",
            "concluded" => "Concluído",
            "done" => "Concluído",
            "cancelled" => "Cancelado",
            "canceled" => "Cancelado",
            _ => string.IsNullOrWhiteSpace(status) ? "Sem status" : status
        };
    }
}