using Amolenk.Admitto.Application.UseCases.Teams.CreateTeam;
using Amolenk.Admitto.Application.UseCases.Teams.GetTeams;

namespace Amolenk.Admitto.ApiService.Endpoints;

public static class TeamEndpoints
{
    public static void MapTeamEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/teams")
            .WithTags("Teams")
            .RequireAuthorization();

        group
            .MapCreateTeam()
            // .MapAddTeamMember()
            .MapGetTeams();
    }
}
