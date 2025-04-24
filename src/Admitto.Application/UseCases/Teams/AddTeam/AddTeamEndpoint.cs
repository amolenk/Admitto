using Amolenk.Admitto.Domain.Entities;

namespace Amolenk.Admitto.Application.UseCases.Teams.AddTeam;

/// <summary>
/// Add a team for organizing events.
/// </summary>
public static class AddTeamEndpoint
{
    public static RouteGroupBuilder MapAddTeam(this RouteGroupBuilder group)
    {
        group.MapPost("/", AddTeam);
        return group;
    }

    private static Created<AddTeamResponse> AddTeam(AddTeamRequest request,
        IDomainContext context, CancellationToken cancellationToken)
    {
        var team = Team.Create(request.Name);
        
        context.Teams.Add(team);

        return TypedResults.Created($"/teams/{team.Id}", AddTeamResponse.FromTeam(team));
    }
}