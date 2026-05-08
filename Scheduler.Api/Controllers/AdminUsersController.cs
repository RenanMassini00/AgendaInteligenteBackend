using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using BCryptNet = BCrypt.Net.BCrypt;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Scheduler.Api.Data;
using Scheduler.Api.DTOs;
using Scheduler.Api.Entities;

namespace Scheduler.Api.Controllers;

[ApiController]
[Route("api/admin/users")]
public class AdminUsersController : ControllerBase
{
    private readonly AppDbContext _context;

    public AdminUsersController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<List<AdminUserResponse>>> GetAll()
    {
        var users = await _context.Users
            .AsNoTracking()
            .Where(x => x.Role == "professional")
            .OrderBy(x => x.BusinessName)
            .ThenBy(x => x.FullName)
            .ToListAsync();

        var result = users.Select(user => new AdminUserResponse(
            user.Id,
            user.FullName,
            user.BusinessName,
            user.Email,
            user.Phone,
            user.Specialty,
            user.Role,
            user.IsActive ? "active" : "inactive",
            user.CreatedAt.ToString("dd/MM/yyyy"),
            user.PublicSlug,
            user.Timezone
        )).ToList();

        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<AdminUserResponse>> GetById(ulong id)
    {
        var user = await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id && x.Role == "professional");

        if (user is null)
        {
            return NotFound(new ApiMessage("Usuário não encontrado."));
        }

        return Ok(new AdminUserResponse(
            user.Id,
            user.FullName,
            user.BusinessName,
            user.Email,
            user.Phone,
            user.Specialty,
            user.Role,
            user.IsActive ? "active" : "inactive",
            user.CreatedAt.ToString("dd/MM/yyyy"),
            user.PublicSlug,
            user.Timezone
        ));
    }

    [HttpPost]
    public async Task<ActionResult<ApiMessage>> Create([FromBody] AdminUserCreateRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.FullName))
        {
            return BadRequest(new ApiMessage("Nome do usuário é obrigatório."));
        }

        if (string.IsNullOrWhiteSpace(request.Email))
        {
            return BadRequest(new ApiMessage("E-mail é obrigatório."));
        }

        if (string.IsNullOrWhiteSpace(request.Password))
        {
            return BadRequest(new ApiMessage("Senha é obrigatória."));
        }

        var normalizedEmail = request.Email.Trim().ToLower();

        var emailAlreadyExists = await _context.Users
            .AnyAsync(x => x.Email == normalizedEmail);

        if (emailAlreadyExists)
        {
            return Conflict(new ApiMessage("Já existe um usuário com esse e-mail."));
        }

        var baseSlugSource = !string.IsNullOrWhiteSpace(request.BusinessName)
            ? request.BusinessName!
            : request.FullName;

        var generatedSlug = await GenerateUniqueSlugAsync(
            request.PublicSlug,
            baseSlugSource,
            null
        );

        var user = new User
        {
            FullName = request.FullName.Trim(),
            BusinessName = string.IsNullOrWhiteSpace(request.BusinessName) ? null : request.BusinessName.Trim(),
            Email = normalizedEmail,
            Phone = string.IsNullOrWhiteSpace(request.Phone) ? null : request.Phone.Trim(),
            Specialty = string.IsNullOrWhiteSpace(request.Specialty) ? null : request.Specialty.Trim(),
            PasswordHash = BCryptNet.HashPassword(request.Password),
            Role = "professional",
            PublicSlug = generatedSlug,
            Timezone = string.IsNullOrWhiteSpace(request.Timezone) ? "America/Sao_Paulo" : request.Timezone.Trim(),
            IsActive = true,
            CreatedAt = DateTime.Now,
            UpdatedAt = DateTime.Now
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        return Ok(new ApiMessage("Usuário profissional cadastrado com sucesso."));
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<ApiMessage>> Update(ulong id, [FromBody] AdminUserUpdateRequest request)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(x => x.Id == id && x.Role == "professional");

        if (user is null)
        {
            return NotFound(new ApiMessage("Usuário não encontrado."));
        }

        if (string.IsNullOrWhiteSpace(request.FullName))
        {
            return BadRequest(new ApiMessage("Nome do usuário é obrigatório."));
        }

        if (string.IsNullOrWhiteSpace(request.Email))
        {
            return BadRequest(new ApiMessage("E-mail é obrigatório."));
        }

        var normalizedEmail = request.Email.Trim().ToLower();

        var emailAlreadyExists = await _context.Users
            .AnyAsync(x => x.Id != id && x.Email == normalizedEmail);

        if (emailAlreadyExists)
        {
            return Conflict(new ApiMessage("Já existe outro usuário com esse e-mail."));
        }

        var baseSlugSource = !string.IsNullOrWhiteSpace(request.BusinessName)
            ? request.BusinessName!
            : request.FullName;

        var generatedSlug = await GenerateUniqueSlugAsync(
            request.PublicSlug,
            baseSlugSource,
            id
        );

        user.FullName = request.FullName.Trim();
        user.BusinessName = string.IsNullOrWhiteSpace(request.BusinessName) ? null : request.BusinessName.Trim();
        user.Email = normalizedEmail;
        user.Phone = string.IsNullOrWhiteSpace(request.Phone) ? null : request.Phone.Trim();
        user.Specialty = string.IsNullOrWhiteSpace(request.Specialty) ? null : request.Specialty.Trim();
        user.PublicSlug = generatedSlug;
        user.Timezone = string.IsNullOrWhiteSpace(request.Timezone) ? "America/Sao_Paulo" : request.Timezone.Trim();
        user.IsActive = request.IsActive;
        user.UpdatedAt = DateTime.Now;

        if (!string.IsNullOrWhiteSpace(request.Password))
        {
            user.PasswordHash = BCryptNet.HashPassword(request.Password);
        }

        await _context.SaveChangesAsync();

        return Ok(new ApiMessage("Usuário atualizado com sucesso."));
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult<ApiMessage>> Delete(ulong id)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(x => x.Id == id && x.Role == "professional");

        if (user is null)
        {
            return NotFound(new ApiMessage("Usuário não encontrado."));
        }

        user.IsActive = false;
        user.UpdatedAt = DateTime.Now;

        await _context.SaveChangesAsync();

        return Ok(new ApiMessage("Usuário inativado com sucesso."));
    }

    private async Task<string> GenerateUniqueSlugAsync(string? preferredSlug, string fallbackText, ulong? ignoreUserId)
    {
        var baseSlug = NormalizeSlug(!string.IsNullOrWhiteSpace(preferredSlug) ? preferredSlug! : fallbackText);

        if (string.IsNullOrWhiteSpace(baseSlug))
        {
            baseSlug = "agenda-profissional";
        }

        var finalSlug = baseSlug;
        var index = 1;

        while (await _context.Users.AnyAsync(x =>
                   x.PublicSlug == finalSlug &&
                   (!ignoreUserId.HasValue || x.Id != ignoreUserId.Value)))
        {
            finalSlug = $"{baseSlug}-{index}";
            index++;
        }

        return finalSlug;
    }

    private static string NormalizeSlug(string value)
    {
        var normalized = value.Trim().ToLowerInvariant().Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder();

        foreach (var character in normalized)
        {
            var unicodeCategory = CharUnicodeInfo.GetUnicodeCategory(character);

            if (unicodeCategory != UnicodeCategory.NonSpacingMark)
            {
                builder.Append(character);
            }
        }

        var clean = builder.ToString().Normalize(NormalizationForm.FormC);
        clean = Regex.Replace(clean, @"[^a-z0-9\s-]", "");
        clean = Regex.Replace(clean, @"\s+", "-");
        clean = Regex.Replace(clean, @"-+", "-");

        return clean.Trim('-');
    }
}