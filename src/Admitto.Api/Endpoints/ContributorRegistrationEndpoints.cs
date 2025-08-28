using Amolenk.Admitto.ApiService.Middleware;
using Amolenk.Admitto.Application.UseCases.Contributors.AddContributor;

namespace Amolenk.Admitto.ApiService.Endpoints;

public static class ContributorRegistrationEndpoints
{
    public static void MapContributorRegistrationEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/teams/{teamSlug}/events/{eventSlug}/contributor-registrations")
            .WithTags("Contributor Registrations")
            .AddEndpointFilter<ValidationFilter>()
            .AddEndpointFilter<UnitOfWorkFilter>()
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status500InternalServerError);

        group
            .MapAddContributorRegistration();
    }
}
