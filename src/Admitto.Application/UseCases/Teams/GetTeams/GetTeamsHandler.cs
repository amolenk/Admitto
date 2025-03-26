using Amolenk.Admitto.Domain.Entities;

namespace Amolenk.Admitto.Application.UseCases.Teams.GetTeams;

/// <summary>
/// Gets all teams that the current user has access to.
/// </summary>
public class GetTeamsHandler(IDomainContext context) 
    : IQueryHandler<GetTeamsQuery, GetTeamsResult>
{
    public async ValueTask<GetTeamsResult> HandleAsync(GetTeamsQuery query, CancellationToken cancellationToken)
    {
        IEnumerable<OrganizingTeam> teams = [];
        //     OrganizingTeam.Create("Dutch IT Events"),
        //     OrganizingTeam.Create("Microsoft Community")
        // ];
//        var teams = await context.OrganizingTeams.ToListAsync(cancellationToken);

        return GetTeamsResult.FromTeams(teams);
    }
}
