using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Scheduler.Api.Data;
using Scheduler.Api.DTOs;
using Scheduler.Api.Entities;

namespace Scheduler.Api.Controllers;

[ApiController]
[Route("api/clients")]
public class ClientsController : ControllerBase
{
    private readonly AppDbContext _context;

    public ClientsController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<ClientResponse>>> GetAll([FromQuery] ulong userId = 1)
    {
        var clients = await _context.Clients
            .AsNoTracking()
            .Where(x => x.UserId == userId && x.IsActive)
            .OrderBy(x => x.FullName)
            .Select(x => ToResponse(x))
            .ToListAsync();

        return Ok(clients);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ClientResponse>> GetById(ulong id)
    {
        var client = await _context.Clients.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id && x.IsActive);
        if (client is null) return NotFound(new ApiMessage("Cliente não encontrado."));
        return Ok(ToResponse(client));
    }

    [HttpPost]
    public async Task<ActionResult<ClientResponse>> Create(ClientCreateRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name) || string.IsNullOrWhiteSpace(request.Phone))
            return BadRequest(new ApiMessage("Nome e telefone são obrigatórios."));

        var client = new Client
        {
            UserId = request.UserId == 0 ? 1 : request.UserId,
            FullName = request.Name.Trim(),
            Email = request.Email,
            Phone = request.Phone.Trim(),
            BirthDate = request.BirthDate,
            Notes = request.Notes,
            IsActive = true,
            CreatedAt = DateTime.Now,
            UpdatedAt = DateTime.Now
        };

        _context.Clients.Add(client);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = client.Id }, ToResponse(client));
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<ClientResponse>> Update(ulong id, ClientUpdateRequest request)
    {
        var client = await _context.Clients.FirstOrDefaultAsync(x => x.Id == id);
        if (client is null) return NotFound(new ApiMessage("Cliente não encontrado."));

        client.FullName = request.Name.Trim();
        client.Email = request.Email;
        client.Phone = request.Phone.Trim();
        client.BirthDate = request.BirthDate;
        client.Notes = request.Notes;
        client.IsActive = request.IsActive;
        client.UpdatedAt = DateTime.Now;

        await _context.SaveChangesAsync();
        return Ok(ToResponse(client));
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult<ApiMessage>> Delete(ulong id)
    {
        var client = await _context.Clients.FirstOrDefaultAsync(x => x.Id == id);
        if (client is null) return NotFound(new ApiMessage("Cliente não encontrado."));

        client.IsActive = false;
        client.UpdatedAt = DateTime.Now;
        await _context.SaveChangesAsync();

        return Ok(new ApiMessage("Cliente removido com sucesso."));
    }

    private static ClientResponse ToResponse(Client client) => new(
        client.Id,
        client.FullName,
        client.Email,
        client.Phone,
        client.BirthDate,
        client.Notes
    );
}
