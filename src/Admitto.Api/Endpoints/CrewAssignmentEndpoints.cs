using Amolenk.Admitto.ApiService.Middleware;
using Amolenk.Admitto.Application.UseCases.CrewAssignments.AddCrewAssignment;

namespace Amolenk.Admitto.ApiService.Endpoints;

public static class CrewAssignmentEndpoints
{
    public static void MapCrewAssignmentEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/teams/{teamSlug}/events/{eventSlug}/crew-assignments")
            .WithTags("Crew Assignments")
            .AddEndpointFilter<ValidationFilter>()
            .AddEndpointFilter<UnitOfWorkFilter>()
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status500InternalServerError);

        group
            .MapAddCrewAssignment();
    }
}
