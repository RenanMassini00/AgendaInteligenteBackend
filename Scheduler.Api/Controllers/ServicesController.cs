using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Scheduler.Api.Data;
using Scheduler.Api.DTOs;
using Scheduler.Api.Entities;
using System.Globalization;

namespace Scheduler.Api.Controllers;

[ApiController]
[Route("api/services")]
[Authorize(Roles = "professional,employee")]
public class ServicesController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly AuthenticatedUserScope _userScope;

    public ServicesController(AppDbContext context, AuthenticatedUserScope userScope)
    {
        _context = context;
        _userScope = userScope;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<ServiceResponse>>> GetAll([FromQuery] ulong userId = 0)
    {
        var user = await _userScope.GetCurrentUserAsync(User);
        if (user is null)
            return Unauthorized(new ApiMessage("Usuário não encontrado ou inativo."));

        var owner = await _userScope.GetBusinessOwnerAsync(user);
        var professionalId = await _userScope.ResolveProfessionalIdAsync(user, userId);
        if (owner is null || professionalId is null)
            return Forbid();

        var items = await _context.Services
            .AsNoTracking()
            .Where(x => x.UserId == owner.Id && x.IsActive)
            .OrderBy(x => x.Name)
            .ToListAsync();

        return Ok(items.Select(ToResponse));
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ServiceResponse>> GetById(ulong id)
    {
        var user = await _userScope.GetCurrentUserAsync(User);
        if (user is null)
            return Unauthorized(new ApiMessage("Usuário não encontrado ou inativo."));

        var owner = await _userScope.GetBusinessOwnerAsync(user);
        if (owner is null)
            return Forbid();

        var service = await _context.Services.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id && x.UserId == owner.Id && x.IsActive);
        if (service is null) return NotFound(new ApiMessage("Serviço não encontrado."));
        return Ok(ToResponse(service));
    }

    [HttpPost]
    [Authorize(Roles = "professional")]
    public async Task<ActionResult<ServiceResponse>> Create(ServiceCreateRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            return BadRequest(new ApiMessage("Nome do serviço é obrigatório."));

        if (request.DurationMinutes <= 0)
            return BadRequest(new ApiMessage("Duração deve ser maior que zero."));

        var user = await _userScope.GetCurrentUserAsync(User);
        if (user is null)
            return Unauthorized(new ApiMessage("Usuário não encontrado ou inativo."));

        var service = new Service
        {
            UserId = user.Id,
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
    [Authorize(Roles = "professional")]
    public async Task<ActionResult<ServiceResponse>> Update(ulong id, ServiceUpdateRequest request)
    {
        var user = await _userScope.GetCurrentUserAsync(User);
        if (user is null)
            return Unauthorized(new ApiMessage("Usuário não encontrado ou inativo."));

        var service = await _context.Services.FirstOrDefaultAsync(x => x.Id == id && x.UserId == user.Id);
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
    [Authorize(Roles = "professional")]
    public async Task<ActionResult<ApiMessage>> Delete(ulong id)
    {
        var user = await _userScope.GetCurrentUserAsync(User);
        if (user is null)
            return Unauthorized(new ApiMessage("Usuário não encontrado ou inativo."));

        var service = await _context.Services.FirstOrDefaultAsync(x => x.Id == id && x.UserId == user.Id);
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
