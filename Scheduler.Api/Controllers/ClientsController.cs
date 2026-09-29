using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Scheduler.Api.Data;
using Scheduler.Api.DTOs;
using Scheduler.Api.Entities;

namespace Scheduler.Api.Controllers;

[ApiController]
[Route("api/clients")]
[Authorize(Roles = "professional,employee")]
public class ClientsController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly AuthenticatedUserScope _userScope;

    public ClientsController(AppDbContext context, AuthenticatedUserScope userScope)
    {
        _context = context;
        _userScope = userScope;
    }

    [HttpGet]
    public async Task<ActionResult<List<ClientResponse>>> GetAll([FromQuery] ulong userId = 0)
    {
        var user = await _userScope.GetCurrentUserAsync(User);
        if (user is null)
            return Unauthorized(new ApiMessage("Usuário não encontrado ou inativo."));

        var owner = await _userScope.GetBusinessOwnerAsync(user);
        var professionalId = await _userScope.ResolveProfessionalIdAsync(user, userId);
        if (owner is null || professionalId is null)
            return Forbid();

        var clientQuery = _context.Clients
            .AsNoTracking()
            .Where(x => x.UserId == owner.Id && x.IsActive);

        if (professionalId != owner.Id)
        {
            var clientIds = _context.Appointments
                .Where(x => x.UserId == professionalId.Value)
                .Select(x => x.ClientId)
                .Distinct();
            clientQuery = clientQuery.Where(x => clientIds.Contains(x.Id));
        }

        var clients = await clientQuery
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
        var user = await _userScope.GetCurrentUserAsync(User);
        if (user is null)
            return Unauthorized(new ApiMessage("Usuário não encontrado ou inativo."));

        var owner = await _userScope.GetBusinessOwnerAsync(user);
        if (owner is null)
            return Forbid();

        var client = await _context.Clients
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id && x.UserId == owner.Id && x.IsActive);

        if (client is null || (user.Role == "employee" &&
            !await _context.Appointments.AnyAsync(x => x.UserId == user.Id && x.ClientId == id)))
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
    [Authorize(Roles = "professional")]
    public async Task<ActionResult<ApiMessage>> Create([FromBody] ClientCreateRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.FullName))
        {
            return BadRequest(new ApiMessage("Nome do cliente é obrigatório."));
        }

        if (string.IsNullOrWhiteSpace(request.Phone))
        {
            return BadRequest(new ApiMessage("Telefone é obrigatório."));
        }

        var user = await _userScope.GetCurrentUserAsync(User);
        if (user is null)
            return Unauthorized(new ApiMessage("Usuário não encontrado ou inativo."));

        var client = new Client
        {
            UserId = user.Id,
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
    [Authorize(Roles = "professional")]
    public async Task<ActionResult<ApiMessage>> Update(ulong id, [FromBody] ClientUpdateRequest request)
    {
        var user = await _userScope.GetCurrentUserAsync(User);
        if (user is null)
            return Unauthorized(new ApiMessage("Usuário não encontrado ou inativo."));

        var client = await _context.Clients.FirstOrDefaultAsync(x => x.Id == id && x.UserId == user.Id);

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
    [Authorize(Roles = "professional")]
    public async Task<ActionResult<ApiMessage>> Delete(ulong id)
    {
        var user = await _userScope.GetCurrentUserAsync(User);
        if (user is null)
            return Unauthorized(new ApiMessage("Usuário não encontrado ou inativo."));

        var client = await _context.Clients.FirstOrDefaultAsync(x => x.Id == id && x.UserId == user.Id);

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