namespace Amolenk.Admitto.Application.UseCases.Teams.GetTeams;

/// <summary>
/// Get all existing teams.
/// </summary>
public static class GetTeamsEndpoint
{
    public static RouteGroupBuilder MapGetTeams(this RouteGroupBuilder group)
    {
        group.MapGet("/teams", GetTeams).WithName(nameof(GetTeams));

        return group;
    }
    
    private static async ValueTask<Ok<GetTeamsResponse>> GetTeams(IDomainContext context, 
        CancellationToken cancellationToken)
    {
        var teams = await context.Teams.ToListAsync(cancellationToken);

        return TypedResults.Ok(GetTeamsResponse.FromTeams(teams));
    }
}
