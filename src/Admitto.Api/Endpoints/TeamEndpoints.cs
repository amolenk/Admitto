using Amolenk.Admitto.ApiService.Middleware;
using Amolenk.Admitto.Application.UseCases.Email.ConfigureTeamEmailTemplate;
using Amolenk.Admitto.Application.UseCases.Teams.AddTeamMember;
using Amolenk.Admitto.Application.UseCases.Teams.CreateTeam;
using Amolenk.Admitto.Application.UseCases.Teams.GetTeam;
using Amolenk.Admitto.Application.UseCases.Teams.GetTeams;

namespace Amolenk.Admitto.ApiService.Endpoints;

public static class TeamEndpoints
{
    public static void MapTeamEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/teams")
            .WithTags("Teams")
            .AddEndpointFilter<ValidationFilter>()
            .AddEndpointFilter<UnitOfWorkFilter>()
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status500InternalServerError)
            .RequireAuthorization();

        group
            .MapCreateTeam()
            .MapGetTeam()
            .MapGetTeams()
            .MapAddTeamMember();
    }
}
