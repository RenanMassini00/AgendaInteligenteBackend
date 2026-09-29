using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Scheduler.Api.Data;
using Scheduler.Api.Entities;

namespace Scheduler.Api.Services;

public sealed class AuthenticatedUserScope
{
    private readonly AppDbContext _context;

    public AuthenticatedUserScope(AppDbContext context)
    {
        _context = context;
    }

    public Task<User?> GetCurrentUserAsync(ClaimsPrincipal principal)
    {
        var userId = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!ulong.TryParse(userId, out var parsedUserId))
            return Task.FromResult<User?>(null);

        return _context.Users.FirstOrDefaultAsync(x => x.Id == parsedUserId && x.IsActive);
    }

    public async Task<User?> GetBusinessOwnerAsync(User user)
    {
        if (user.Role == "professional")
            return user.HasAppointmentsModule ? user : null;

        if (user.Role != "employee" || user.TeamOwnerUserId is null)
            return null;

        return await _context.Users.FirstOrDefaultAsync(x =>
            x.Id == user.TeamOwnerUserId.Value &&
            x.Role == "professional" &&
            x.IsActive &&
            x.HasAppointmentsModule);
    }

    public async Task<ulong?> ResolveProfessionalIdAsync(User user, ulong requestedUserId)
    {
        if (user.Role == "employee")
            return user.Id;

        if (user.Role != "professional")
            return null;

        var professionalId = requestedUserId == 0 ? user.Id : requestedUserId;
        if (professionalId == user.Id)
            return user.Id;

        var isActiveTeamMember = await _context.Users.AnyAsync(x =>
            x.Id == professionalId &&
            x.Role == "employee" &&
            x.TeamOwnerUserId == user.Id &&
            x.IsActive);

        return isActiveTeamMember ? professionalId : null;
    }

    public async Task<List<ulong>> GetAccessibleProfessionalIdsAsync(User user)
    {
        if (user.Role == "employee")
            return [user.Id];

        if (user.Role != "professional")
            return [];

        var employeeIds = await _context.Users
            .AsNoTracking()
            .Where(x => x.TeamOwnerUserId == user.Id && x.Role == "employee" && x.IsActive)
            .Select(x => x.Id)
            .ToListAsync();

        employeeIds.Add(user.Id);
        return employeeIds;
    }
}
