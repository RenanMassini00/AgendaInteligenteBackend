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
    public async Task<ActionResult<List<ClientResponse>>> GetAll([FromQuery] ulong userId = 1)
    {
        var clients = await _context.Clients
            .AsNoTracking()
            .Where(x => x.UserId == userId && x.IsActive)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync();

        var result = clients.Select(client => new ClientResponse(
            client.Id,
            client.FullName,
            client.Email,
            client.Phone,
            client.BirthDate.HasValue ? client.BirthDate.Value.ToString("yyyy-MM-dd") : null,
            client.Notes,
            client.IsActive ? "active" : "inactive",
            client.CreatedAt.ToString("dd/MM/yyyy")
        )).ToList();

        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ClientResponse>> GetById(ulong id)
    {
        var client = await _context.Clients
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id);

        if (client is null)
        {
            return NotFound(new ApiMessage("Cliente não encontrado."));
        }

        return Ok(new ClientResponse(
            client.Id,
            client.FullName,
            client.Email,
            client.Phone,
            client.BirthDate.HasValue ? client.BirthDate.Value.ToString("yyyy-MM-dd") : null,
            client.Notes,
            client.IsActive ? "active" : "inactive",
            client.CreatedAt.ToString("dd/MM/yyyy")
        ));
    }

    [HttpPost]
    public async Task<ActionResult<ApiMessage>> Create([FromBody] ClientCreateRequest request, [FromQuery] ulong userId = 1)
    {
        if (string.IsNullOrWhiteSpace(request.FullName))
        {
            return BadRequest(new ApiMessage("Nome do cliente é obrigatório."));
        }

        if (string.IsNullOrWhiteSpace(request.Phone))
        {
            return BadRequest(new ApiMessage("Telefone é obrigatório."));
        }

        var client = new Client
        {
            UserId = userId,
            FullName = request.FullName.Trim(),
            Email = string.IsNullOrWhiteSpace(request.Email) ? null : request.Email.Trim(),
            Phone = request.Phone.Trim(),
            BirthDate = request.BirthDate,
            Notes = string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim(),
            IsActive = true,
            CreatedAt = DateTime.Now,
            UpdatedAt = DateTime.Now
        };

        _context.Clients.Add(client);
        await _context.SaveChangesAsync();

        return Ok(new ApiMessage("Cliente cadastrado com sucesso."));
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<ApiMessage>> Update(ulong id, [FromBody] ClientUpdateRequest request)
    {
        var client = await _context.Clients.FirstOrDefaultAsync(x => x.Id == id);

        if (client is null)
        {
            return NotFound(new ApiMessage("Cliente não encontrado."));
        }

        if (string.IsNullOrWhiteSpace(request.FullName))
        {
            return BadRequest(new ApiMessage("Nome do cliente é obrigatório."));
        }

        if (string.IsNullOrWhiteSpace(request.Phone))
        {
            return BadRequest(new ApiMessage("Telefone é obrigatório."));
        }

        client.FullName = request.FullName.Trim();
        client.Email = string.IsNullOrWhiteSpace(request.Email) ? null : request.Email.Trim();
        client.Phone = request.Phone.Trim();
        client.BirthDate = request.BirthDate;
        client.Notes = string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim();
        client.UpdatedAt = DateTime.Now;

        await _context.SaveChangesAsync();

        return Ok(new ApiMessage("Cliente atualizado com sucesso."));
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult<ApiMessage>> Delete(ulong id)
    {
        var client = await _context.Clients.FirstOrDefaultAsync(x => x.Id == id);

        if (client is null)
        {
            return NotFound(new ApiMessage("Cliente não encontrado."));
        }

        client.IsActive = false;
        client.UpdatedAt = DateTime.Now;

        await _context.SaveChangesAsync();

        return Ok(new ApiMessage("Cliente excluído com sucesso."));
    }
}