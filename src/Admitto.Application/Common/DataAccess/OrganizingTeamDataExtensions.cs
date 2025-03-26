using Amolenk.Admitto.Domain.Entities;

namespace Amolenk.Admitto.Application.Common.DataAccess;

public static class OrganizingTeamDataExtensions
{
    public static async ValueTask<OrganizingTeam> GetByIdAsync(this DbSet<OrganizingTeam> teams, Guid id, 
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