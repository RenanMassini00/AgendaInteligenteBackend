using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Scheduler.Api.Data;
using Scheduler.Api.DTOs;
using Scheduler.Api.Entities;
using System.Globalization;

namespace Scheduler.Api.Controllers;

[ApiController]
[Route("api/services")]
public class ServicesController : ControllerBase
{
    private readonly AppDbContext _context;

    public ServicesController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<ServiceResponse>>> GetAll([FromQuery] ulong userId = 1)
    {
        var items = await _context.Services
            .AsNoTracking()
            .Where(x => x.UserId == userId && x.IsActive)
            .OrderBy(x => x.Name)
            .ToListAsync();

        return Ok(items.Select(ToResponse));
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ServiceResponse>> GetById(ulong id)
    {
        var service = await _context.Services.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id && x.IsActive);
        if (service is null) return NotFound(new ApiMessage("Serviço não encontrado."));
        return Ok(ToResponse(service));
    }

    [HttpPost]
    public async Task<ActionResult<ServiceResponse>> Create(ServiceCreateRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            return BadRequest(new ApiMessage("Nome do serviço é obrigatório."));

        if (request.DurationMinutes <= 0)
            return BadRequest(new ApiMessage("Duração deve ser maior que zero."));

        var service = new Service
        {
            UserId = request.UserId == 0 ? 1 : request.UserId,
            Name = request.Name.Trim(),
            Description = request.Description,
            DurationMinutes = request.DurationMinutes,
            Price = request.Price,
            ColorHex = request.ColorHex,
            IsActive = true,
            CreatedAt = DateTime.Now,
            UpdatedAt = DateTime.Now
        };

        _context.Services.Add(service);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = service.Id }, ToResponse(service));
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<ServiceResponse>> Update(ulong id, ServiceUpdateRequest request)
    {
        var service = await _context.Services.FirstOrDefaultAsync(x => x.Id == id);
        if (service is null) return NotFound(new ApiMessage("Serviço não encontrado."));

        if (request.DurationMinutes <= 0)
            return BadRequest(new ApiMessage("Duração deve ser maior que zero."));

        service.Name = request.Name.Trim();
        service.Description = request.Description;
        service.DurationMinutes = request.DurationMinutes;
        service.Price = request.Price;
        service.ColorHex = request.ColorHex;
        service.IsActive = request.IsActive;
        service.UpdatedAt = DateTime.Now;

        await _context.SaveChangesAsync();
        return Ok(ToResponse(service));
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult<ApiMessage>> Delete(ulong id)
    {
        var service = await _context.Services.FirstOrDefaultAsync(x => x.Id == id);
        if (service is null) return NotFound(new ApiMessage("Serviço não encontrado."));

        service.IsActive = false;
        service.UpdatedAt = DateTime.Now;
        await _context.SaveChangesAsync();

        return Ok(new ApiMessage("Serviço removido com sucesso."));
    }

    private static ServiceResponse ToResponse(Service service) => new(
        service.Id,
        service.Name,
        service.Description,
        service.DurationMinutes,
        FormatDuration(service.DurationMinutes),
        service.Price,
        service.Price.ToString("C", CultureInfo.GetCultureInfo("pt-BR")),
        service.ColorHex
    );

    private static string FormatDuration(int minutes)
    {
        if (minutes < 60) return $"{minutes} min";
        var hours = minutes / 60;
        var rest = minutes % 60;
        return rest == 0 ? $"{hours}h" : $"{hours}h {rest}min";
    }
}
