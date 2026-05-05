using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Scheduler.Api.Data;
using Scheduler.Api.DTOs;

namespace Scheduler.Api.Controllers;

[ApiController]
[Route("api/profile")]
public class ProfileController : ControllerBase
{
    private readonly AppDbContext _context;

    public ProfileController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<UserResponse>> Get([FromQuery] ulong userId = 1)
    {
        var user = await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == userId && x.IsActive);

        if (user is null)
        {
            return NotFound(new ApiMessage("Usuário não encontrado."));
        }

        return Ok(new UserResponse(
            user.Id,
            user.FullName,
            user.BusinessName,
            user.Email,
            user.Phone,
            user.Specialty,
            user.Timezone,
            user.Role,
            user.ProfessionalUserId,
            user.ClientId,
            user.PublicSlug
        ));
    }
}