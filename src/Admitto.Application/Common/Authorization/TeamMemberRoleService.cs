using Amolenk.Admitto.Application.Common.Persistence;
using Amolenk.Admitto.Domain.ValueObjects;

namespace Amolenk.Admitto.Application.Common.Authorization;

public class TeamMemberRoleService(IApplicationContext context) : ITeamMemberRoleService
{
    public async ValueTask<IEnumerable<Guid>> GetTeamsAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await context.TeamMemberView
            .AsNoTracking()
            .Where(v => v.UserId == userId)
            .Select(v => v.TeamId)
            .Distinct()
            .ToListAsync(cancellationToken);
    }

    public async ValueTask<TeamMemberRole?> GetTeamMemberRoleAsync(
        Guid userId,
        Guid teamId,
        CancellationToken cancellationToken = default)
    {
        return await context.TeamMemberView
            .AsNoTracking()
            .Where(v => v.TeamId == teamId && v.UserId == userId)
            .Select(v => v.Role)
            .FirstOrDefaultAsync(cancellationToken);
    }
}