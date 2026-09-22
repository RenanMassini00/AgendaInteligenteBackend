using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Scheduler.Api.Data;
using Scheduler.Api.DTOs;

namespace Scheduler.Api.Controllers;

[ApiController]
[Route("api/notifications")]
public class NotificationsController : ControllerBase
{
    private readonly AppDbContext _context;

    public NotificationsController(AppDbContext context) => _context = context;

    [HttpGet]
    public async Task<ActionResult<IEnumerable<NotificationResponse>>> Get(
        [FromQuery] ulong userId,
        [FromQuery] bool unreadOnly = false)
    {
        if (userId == 0) return BadRequest(new ApiMessage("Usuário inválido."));

        var query = _context.AppNotifications
            .AsNoTracking()
            .Where(x => x.UserId == userId);
        if (unreadOnly) query = query.Where(x => !x.IsRead);

        var items = await query.OrderByDescending(x => x.CreatedAt).Take(100).ToListAsync();
        return Ok(items.Select(ToResponse));
    }

    [HttpPatch("{id}/read")]
    public async Task<ActionResult<ApiMessage>> MarkRead(ulong id, [FromQuery] ulong userId)
    {
        var notification = await _context.AppNotifications
            .FirstOrDefaultAsync(x => x.Id == id && x.UserId == userId);
        if (notification is null) return NotFound(new ApiMessage("Notificação não encontrada."));

        notification.IsRead = true;
        notification.ReadAt = DateTime.Now;
        await _context.SaveChangesAsync();
        return Ok(new ApiMessage("Notificação marcada como lida."));
    }

    private static NotificationResponse ToResponse(Entities.AppNotification notification) => new(
        notification.Id, notification.AppointmentId, notification.Type, notification.Title,
        notification.Message, notification.ActionUrl, notification.CalendarUrl,
        notification.IsRead, notification.CreatedAt);
}
