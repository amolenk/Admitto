using Amolenk.Admitto.Application.Common.Validation;

namespace Amolenk.Admitto.Application.Common.Data;

public static class TeamSetExtensions
{
    public static async ValueTask<Guid> GetTeamIdAsync(this DbSet<Domain.Entities.Team> teams, string teamSlug,
        CancellationToken cancellationToken = default)
    {
        var teamId = await teams
            .AsNoTracking()
            .Where(t => t.Slug == teamSlug)
            .Select(t => t.Id)
            .FirstOrDefaultAsync(cancellationToken);
        
        if (teamId == Guid.Empty)
        {
            throw ValidationError.Team.NotFound(teamSlug);
        }
        
        return teamId;
    }
}