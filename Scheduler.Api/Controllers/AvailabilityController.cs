using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Scheduler.Api.Data;
using Scheduler.Api.DTOs;
using Scheduler.Api.Entities;

namespace Scheduler.Api.Controllers;

[ApiController]
[Route("api/availability")]
public class AvailabilityController : ControllerBase
{
    private readonly AppDbContext _context;

    public AvailabilityController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<List<AvailabilityResponse>>> GetAll([FromQuery] ulong userId = 1)
    {
        var items = await _context.WeeklyAvailabilities
            .AsNoTracking()
            .Where(x => x.UserId == userId)
            .OrderBy(x => x.Weekday)
            .ThenBy(x => x.StartTime)
            .ToListAsync();

        var response = items
            .Select(x => new AvailabilityResponse(
                x.Id,
                x.Weekday,
                GetWeekdayName(x.Weekday),
                x.StartTime.ToString(@"hh\:mm"),
                x.EndTime.ToString(@"hh\:mm"),
                x.IsActive
            ))
            .ToList();

        return Ok(response);
    }

    [HttpPost]
    public async Task<ActionResult<AvailabilityResponse>> Create([FromBody] AvailabilityCreateRequest request)
    {
        if (!TimeSpan.TryParseExact(request.StartTime, @"hh\:mm", CultureInfo.InvariantCulture, out var startTime))
        {
            return BadRequest(new ApiMessage("Hora inicial inválida. Use o formato HH:mm."));
        }

        if (!TimeSpan.TryParseExact(request.EndTime, @"hh\:mm", CultureInfo.InvariantCulture, out var endTime))
        {
            return BadRequest(new ApiMessage("Hora final inválida. Use o formato HH:mm."));
        }

        if (startTime >= endTime)
        {
            return BadRequest(new ApiMessage("A hora final deve ser maior que a hora inicial."));
        }

        var userExists = await _context.Users
            .AsNoTracking()
            .AnyAsync(x => x.Id == request.UserId && x.IsActive);

        if (!userExists)
        {
            return NotFound(new ApiMessage("Usuário não encontrado."));
        }

        var hasConflict = await _context.WeeklyAvailabilities
            .AsNoTracking()
            .AnyAsync(x =>
                x.UserId == request.UserId &&
                x.Weekday == request.Weekday &&
                startTime < x.EndTime &&
                endTime > x.StartTime);

        if (hasConflict)
        {
            return Conflict(new ApiMessage("Já existe uma recorrência conflitante nesse dia."));
        }

        var entity = new WeeklyAvailability
        {
            UserId = request.UserId,
            Weekday = request.Weekday,
            StartTime = startTime,
            EndTime = endTime,
            IsActive = request.IsActive,
            CreatedAt = DateTime.Now,
            UpdatedAt = DateTime.Now
        };

        _context.WeeklyAvailabilities.Add(entity);
        await _context.SaveChangesAsync();

        return Ok(new AvailabilityResponse(
            entity.Id,
            entity.Weekday,
            GetWeekdayName(entity.Weekday),
            entity.StartTime.ToString(@"hh\:mm"),
            entity.EndTime.ToString(@"hh\:mm"),
            entity.IsActive
        ));
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<AvailabilityResponse>> Update(ulong id, [FromBody] AvailabilityUpdateRequest request)
    {
        var entity = await _context.WeeklyAvailabilities
            .FirstOrDefaultAsync(x => x.Id == id);

        if (entity is null)
        {
            return NotFound(new ApiMessage("Recorrência não encontrada."));
        }

        if (!TimeSpan.TryParseExact(request.StartTime, @"hh\:mm", CultureInfo.InvariantCulture, out var startTime))
        {
            return BadRequest(new ApiMessage("Hora inicial inválida. Use o formato HH:mm."));
        }

        if (!TimeSpan.TryParseExact(request.EndTime, @"hh\:mm", CultureInfo.InvariantCulture, out var endTime))
        {
            return BadRequest(new ApiMessage("Hora final inválida. Use o formato HH:mm."));
        }

        if (startTime >= endTime)
        {
            return BadRequest(new ApiMessage("A hora final deve ser maior que a hora inicial."));
        }

        var hasConflict = await _context.WeeklyAvailabilities
            .AsNoTracking()
            .AnyAsync(x =>
                x.Id != id &&
                x.UserId == entity.UserId &&
                x.Weekday == request.Weekday &&
                startTime < x.EndTime &&
                endTime > x.StartTime);

        if (hasConflict)
        {
            return Conflict(new ApiMessage("Já existe uma recorrência conflitante nesse dia."));
        }

        entity.Weekday = request.Weekday;
        entity.StartTime = startTime;
        entity.EndTime = endTime;
        entity.IsActive = request.IsActive;
        entity.UpdatedAt = DateTime.Now;

        await _context.SaveChangesAsync();

        return Ok(new AvailabilityResponse(
            entity.Id,
            entity.Weekday,
            GetWeekdayName(entity.Weekday),
            entity.StartTime.ToString(@"hh\:mm"),
            entity.EndTime.ToString(@"hh\:mm"),
            entity.IsActive
        ));
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult<ApiMessage>> Delete(ulong id)
    {
        var entity = await _context.WeeklyAvailabilities
            .FirstOrDefaultAsync(x => x.Id == id);

        if (entity is null)
        {
            return NotFound(new ApiMessage("Recorrência não encontrada."));
        }

        _context.WeeklyAvailabilities.Remove(entity);
        await _context.SaveChangesAsync();

        return Ok(new ApiMessage("Recorrência removida com sucesso."));
    }

    [HttpGet("dates")]
    public async Task<ActionResult<List<AvailabilityDateResponse>>> GetDates([FromQuery] ulong userId = 1)
    {
        var items = await _context.AvailabilityDates
            .AsNoTracking()
            .Where(x => x.UserId == userId)
            .OrderBy(x => x.AvailableDate)
            .ThenBy(x => x.StartTime)
            .ToListAsync();

        var response = items
            .Select(x => new AvailabilityDateResponse(
                x.Id,
                x.AvailableDate.ToString("yyyy-MM-dd"),
                x.StartTime.ToString(@"hh\:mm"),
                x.EndTime.ToString(@"hh\:mm")
            ))
            .ToList();

        return Ok(response);
    }

    [HttpPost("dates")]
    public async Task<ActionResult<AvailabilityDateResponse>> CreateDate(
        [FromQuery] ulong userId,
        [FromBody] CreateAvailabilityDateRequest request)
    {
        if (!TimeSpan.TryParseExact(request.StartTime, @"hh\:mm", CultureInfo.InvariantCulture, out var startTime))
        {
            return BadRequest(new ApiMessage("Hora inicial inválida. Use o formato HH:mm."));
        }

        if (!TimeSpan.TryParseExact(request.EndTime, @"hh\:mm", CultureInfo.InvariantCulture, out var endTime))
        {
            return BadRequest(new ApiMessage("Hora final inválida. Use o formato HH:mm."));
        }

        if (startTime >= endTime)
        {
            return BadRequest(new ApiMessage("A hora final deve ser maior que a hora inicial."));
        }

        var userExists = await _context.Users
            .AsNoTracking()
            .AnyAsync(x => x.Id == userId && x.IsActive);

        if (!userExists)
        {
            return NotFound(new ApiMessage("Usuário não encontrado."));
        }

        var targetDate = request.AvailableDate.Date;

        var hasConflict = await _context.AvailabilityDates
            .AsNoTracking()
            .AnyAsync(x =>
                x.UserId == userId &&
                x.AvailableDate == targetDate &&
                startTime < x.EndTime &&
                endTime > x.StartTime);

        if (hasConflict)
        {
            return Conflict(new ApiMessage("Já existe uma data específica conflitante."));
        }

        var entity = new AvailabilityDate
        {
            UserId = userId,
            AvailableDate = targetDate,
            StartTime = startTime,
            EndTime = endTime,
            CreatedAt = DateTime.Now,
            UpdatedAt = DateTime.Now
        };

        _context.AvailabilityDates.Add(entity);
        await _context.SaveChangesAsync();

        return Ok(new AvailabilityDateResponse(
            entity.Id,
            entity.AvailableDate.ToString("yyyy-MM-dd"),
            entity.StartTime.ToString(@"hh\:mm"),
            entity.EndTime.ToString(@"hh\:mm")
        ));
    }

    [HttpPut("dates/{id}")]
    public async Task<ActionResult<AvailabilityDateResponse>> UpdateDate(
        ulong id,
        [FromBody] UpdateAvailabilityDateRequest request)
    {
        var entity = await _context.AvailabilityDates
            .FirstOrDefaultAsync(x => x.Id == id);

        if (entity is null)
        {
            return NotFound(new ApiMessage("Data específica não encontrada."));
        }

        if (!TimeSpan.TryParseExact(request.StartTime, @"hh\:mm", CultureInfo.InvariantCulture, out var startTime))
        {
            return BadRequest(new ApiMessage("Hora inicial inválida. Use o formato HH:mm."));
        }

        if (!TimeSpan.TryParseExact(request.EndTime, @"hh\:mm", CultureInfo.InvariantCulture, out var endTime))
        {
            return BadRequest(new ApiMessage("Hora final inválida. Use o formato HH:mm."));
        }

        if (startTime >= endTime)
        {
            return BadRequest(new ApiMessage("A hora final deve ser maior que a hora inicial."));
        }

        var targetDate = request.AvailableDate.Date;

        var hasConflict = await _context.AvailabilityDates
            .AsNoTracking()
            .AnyAsync(x =>
                x.Id != id &&
                x.UserId == entity.UserId &&
                x.AvailableDate == targetDate &&
                startTime < x.EndTime &&
                endTime > x.StartTime);

        if (hasConflict)
        {
            return Conflict(new ApiMessage("Já existe uma data específica conflitante."));
        }

        entity.AvailableDate = targetDate;
        entity.StartTime = startTime;
        entity.EndTime = endTime;
        entity.UpdatedAt = DateTime.Now;

        await _context.SaveChangesAsync();

        return Ok(new AvailabilityDateResponse(
            entity.Id,
            entity.AvailableDate.ToString("yyyy-MM-dd"),
            entity.StartTime.ToString(@"hh\:mm"),
            entity.EndTime.ToString(@"hh\:mm")
        ));
    }

    [HttpDelete("dates/{id}")]
    public async Task<ActionResult<ApiMessage>> DeleteDate(ulong id)
    {
        var entity = await _context.AvailabilityDates
            .FirstOrDefaultAsync(x => x.Id == id);

        if (entity is null)
        {
            return NotFound(new ApiMessage("Data específica não encontrada."));
        }

        _context.AvailabilityDates.Remove(entity);
        await _context.SaveChangesAsync();

        return Ok(new ApiMessage("Data específica removida com sucesso."));
    }

    private static string GetWeekdayName(int weekday)
    {
        return weekday switch
        {
            0 => "Domingo",
            1 => "Segunda-feira",
            2 => "Terça-feira",
            3 => "Quarta-feira",
            4 => "Quinta-feira",
            5 => "Sexta-feira",
            6 => "Sábado",
            _ => "Desconhecido"
        };
    }
}