namespace Amolenk.Admitto.Application.UseCases.Teams.GetTeams;

/// <summary>
/// Gets all teams that the current user has access to.
/// </summary>
public class GetTeamsHandler(IDomainContext context) 
    : IQueryHandler<GetTeamsQuery, IEnumerable<TeamDto>>
{
    public async ValueTask<Result<IEnumerable<TeamDto>>> HandleAsync(GetTeamsQuery query, CancellationToken cancellationToken)
    {
        var teams = await context.Teams.ToListAsync(cancellationToken);

        return Result<IEnumerable<TeamDto>>.Success(teams.Select(TeamDto.FromTeam));
    }
}
