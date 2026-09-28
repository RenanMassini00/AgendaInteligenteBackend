using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Scheduler.Api.Data;
using Scheduler.Api.DTOs;
using Scheduler.Api.Entities;

namespace Scheduler.Api.Controllers;

[ApiController]
[Route("api/professional-team/employees")]
[EnableRateLimiting("Auth")]
[Authorize(Roles = "professional")]
public class ProfessionalTeamController : ControllerBase
{
    private static readonly PasswordHasher<User> PasswordHasher = new();
    private readonly AppDbContext _context;

    public ProfessionalTeamController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<List<ProfessionalEmployeeResponse>>> GetAll()
    {
        var owner = await GetActiveOwnerAsync();
        if (owner is null)
            return BadRequest(new ApiMessage("Empresa não encontrada ou sem acesso ao módulo de agendamentos."));

        var employees = await _context.Users
            .AsNoTracking()
            .Where(x => x.TeamOwnerUserId == owner.Id && x.Role == "employee")
            .OrderBy(x => x.FullName)
            .ToListAsync();

        return Ok(employees.Select(ToResponse).ToList());
    }

    [HttpPost]
    public async Task<ActionResult<ProfessionalEmployeeResponse>> Create(
        [FromBody] ProfessionalEmployeeCreateRequest request)
    {
        var owner = await GetActiveOwnerAsync();
        if (owner is null)
            return BadRequest(new ApiMessage("Empresa não encontrada ou sem acesso ao módulo de agendamentos."));

        if (string.IsNullOrWhiteSpace(request.FullName) ||
            string.IsNullOrWhiteSpace(request.Email) ||
            string.IsNullOrWhiteSpace(request.Password))
            return BadRequest(new ApiMessage("Nome, e-mail e senha do funcionário são obrigatórios."));

        var email = request.Email.Trim().ToLowerInvariant();
        if (!new EmailAddressAttribute().IsValid(email))
            return BadRequest(new ApiMessage("Informe um e-mail válido."));

        if (await _context.Users.AnyAsync(x => x.Email == email))
            return Conflict(new ApiMessage("Já existe um usuário com esse e-mail."));

        var now = DateTime.Now;
        var employee = new User
        {
            FullName = request.FullName.Trim(),
            Email = email,
            Phone = request.Phone?.Trim(),
            Specialty = request.Specialty?.Trim(),
            Timezone = string.IsNullOrWhiteSpace(request.Timezone)
                ? owner.Timezone
                : request.Timezone.Trim(),
            PasswordHash = string.Empty,
            Role = "employee",
            TeamOwnerUserId = owner.Id,
            CompanyId = owner.CompanyId,
            HasAppointmentsModule = true,
            HasCatalogModule = false,
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now
        };
        employee.PasswordHash = PasswordHasher.HashPassword(employee, request.Password);

        _context.Users.Add(employee);
        await _context.SaveChangesAsync();

        _context.UserSettings.Add(new UserSetting
        {
            UserId = employee.Id,
            ThemeMode = "light",
            AccentColor = "blue",
            LogoUrl = null,
            LanguageCode = "pt-BR",
            ReminderMinutes = 60,
            EmailNotifications = false,
            WhatsAppNotifications = true,
            CreatedAt = now,
            UpdatedAt = now
        });
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetAll), null, ToResponse(employee));
    }

    [HttpPut("{employeeId}")]
    public async Task<ActionResult<ProfessionalEmployeeResponse>> Update(
        ulong employeeId,
        [FromBody] ProfessionalEmployeeUpdateRequest request)
    {
        var owner = await GetActiveOwnerAsync();
        if (owner is null)
            return BadRequest(new ApiMessage("Empresa não encontrada ou sem acesso ao módulo de agendamentos."));

        var employee = await _context.Users.FirstOrDefaultAsync(x =>
            x.Id == employeeId &&
            x.TeamOwnerUserId == owner.Id &&
            x.Role == "employee");
        if (employee is null)
            return NotFound(new ApiMessage("Funcionário não encontrado."));

        if (string.IsNullOrWhiteSpace(request.FullName) ||
            string.IsNullOrWhiteSpace(request.Email))
            return BadRequest(new ApiMessage("Nome e e-mail do funcionário são obrigatórios."));

        var email = request.Email.Trim().ToLowerInvariant();
        if (!new EmailAddressAttribute().IsValid(email))
            return BadRequest(new ApiMessage("Informe um e-mail válido."));

        if (await _context.Users.AnyAsync(x => x.Id != employee.Id && x.Email == email))
            return Conflict(new ApiMessage("Já existe um usuário com esse e-mail."));

        employee.FullName = request.FullName.Trim();
        employee.Email = email;
        employee.Phone = request.Phone?.Trim();
        employee.Specialty = request.Specialty?.Trim();
        employee.Timezone = string.IsNullOrWhiteSpace(request.Timezone)
            ? owner.Timezone
            : request.Timezone.Trim();
        employee.IsActive = request.IsActive;
        employee.UpdatedAt = DateTime.Now;

        if (!string.IsNullOrWhiteSpace(request.Password))
            employee.PasswordHash = PasswordHasher.HashPassword(employee, request.Password);

        await _context.SaveChangesAsync();
        return Ok(ToResponse(employee));
    }

    [HttpDelete("{employeeId}")]
    public async Task<ActionResult<ApiMessage>> Deactivate(
        ulong employeeId)
    {
        var owner = await GetActiveOwnerAsync();
        if (owner is null)
            return BadRequest(new ApiMessage("Empresa não encontrada ou sem acesso ao módulo de agendamentos."));

        var employee = await _context.Users.FirstOrDefaultAsync(x =>
            x.Id == employeeId &&
            x.TeamOwnerUserId == owner.Id &&
            x.Role == "employee");
        if (employee is null)
            return NotFound(new ApiMessage("Funcionário não encontrado."));

        employee.IsActive = false;
        employee.UpdatedAt = DateTime.Now;
        await _context.SaveChangesAsync();

        return Ok(new ApiMessage("Funcionário inativado com sucesso."));
    }

    private Task<User?> GetActiveOwnerAsync()
    {
        var ownerUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!ulong.TryParse(ownerUserId, out var parsedOwnerUserId))
            return Task.FromResult<User?>(null);

        return _context.Users.FirstOrDefaultAsync(x =>
            x.Id == parsedOwnerUserId &&
            x.Role == "professional" &&
            x.IsActive &&
            x.HasAppointmentsModule);
    }

    private static ProfessionalEmployeeResponse ToResponse(User employee) => new(
        employee.Id,
        employee.Id,
        employee.FullName,
        employee.Email,
        employee.Phone,
        employee.Specialty,
        employee.Timezone,
        employee.IsActive,
        employee.TeamOwnerUserId!.Value
    );
}
