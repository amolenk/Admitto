using Amolenk.Admitto.Domain.Entities;

namespace Amolenk.Admitto.Application.Common.DataAccess;

public static class TeamDataExtensions
{
    public static async ValueTask<Team> GetByIdAsync(this DbSet<Team> teams, Guid id, 
        CancellationToken cancellationToken)
    {
        var team = await teams.FindAsync([id], cancellationToken);
        if (team is null)
        {
            throw new DomainObjectNotFoundException($"Team {id} not found.");
        }
        
        return team;
    }
}