using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Scheduler.Api.Data;
using Scheduler.Api.DTOs;
using System.Globalization;

namespace Scheduler.Api.Controllers;

[ApiController]
[Route("api/dashboard")]
public class DashboardController : ControllerBase
{
    private readonly AppDbContext _context;

    public DashboardController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet("summary")]
    public async Task<ActionResult<DashboardSummaryResponse>> Summary([FromQuery] ulong userId = 1, [FromQuery] string? date = null)
    {
        var culture = CultureInfo.GetCultureInfo("pt-BR");
        var targetDate = DateTime.Today;

        if (!string.IsNullOrWhiteSpace(date) && DateTime.TryParse(date, out var parsedDate))
            targetDate = parsedDate.Date;

        var todayAppointments = await _context.Appointments
            .AsNoTracking()
            .Where(x => x.UserId == userId && x.AppointmentDate.Date == targetDate.Date)
            .ToListAsync();

        var appointmentsToday = todayAppointments.Count;
        var expectedRevenue = todayAppointments.Where(x => x.Status != "cancelled").Sum(x => x.PriceAtBooking);

        var clientsCount = await _context.Clients.CountAsync(x => x.UserId == userId && x.IsActive);
        var servicesCount = await _context.Services.CountAsync(x => x.UserId == userId && x.IsActive);

        var upcomingAppointmentsData = await _context.Appointments
            .AsNoTracking()
            .Include(x => x.Client)
            .Include(x => x.Service)
            .Where(x => x.UserId == userId && x.Status != "cancelled")
            .OrderBy(x => x.AppointmentDate)
            .ThenBy(x => x.StartTime)
            .Take(5)
            .ToListAsync();

        var upcomingAppointments = upcomingAppointmentsData.Select(x => new AppointmentResponse(
            x.Id,
            x.ClientId,
            x.ServiceId,
            x.Client?.FullName ?? string.Empty,
            x.Service?.Name ?? string.Empty,
            x.AppointmentDate.ToString("yyyy-MM-dd"),
            x.StartTime.ToString(@"hh\:mm"),
            x.StartTime.ToString(@"hh\:mm"),
            x.EndTime.ToString(@"hh\:mm"),
            x.Status,
            x.PriceAtBooking,
            x.PriceAtBooking.ToString("C", culture),
            x.Notes
        )).ToList();

        var recentClients = await _context.Clients
            .AsNoTracking()
            .Where(x => x.UserId == userId && x.IsActive)
            .OrderByDescending(x => x.CreatedAt)
            .Take(5)
            .Select(x => new ClientResponse(x.Id, x.FullName, x.Email, x.Phone, x.BirthDate, x.Notes))
            .ToListAsync();

        var topServicesData = await _context.Services
            .AsNoTracking()
            .Where(x => x.UserId == userId && x.IsActive)
            .OrderBy(x => x.Name)
            .Take(5)
            .ToListAsync();

        var topServices = topServicesData.Select(x => new ServiceResponse(
            x.Id,
            x.Name,
            x.Description,
            x.DurationMinutes,
            x.DurationMinutes < 60 ? $"{x.DurationMinutes} min" : $"{x.DurationMinutes / 60}h",
            x.Price,
            x.Price.ToString("C", culture),
            x.ColorHex
        )).ToList();

        return Ok(new DashboardSummaryResponse(
            appointmentsToday,
            clientsCount,
            servicesCount,
            expectedRevenue,
            expectedRevenue.ToString("C", culture),
            upcomingAppointments,
            recentClients,
            topServices
        ));
    }
}
