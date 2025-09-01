using Amolenk.Admitto.ApiService.Middleware;
using Amolenk.Admitto.Application.UseCases.Contributors.AddContributor;
using Amolenk.Admitto.Application.UseCases.Contributors.GetContributors;
using Amolenk.Admitto.Application.UseCases.Contributors.RemoveContributor;
using Amolenk.Admitto.Application.UseCases.Contributors.UpdateContributor;

namespace Amolenk.Admitto.ApiService.Endpoints;

public static class ContributorEndpoints
{
    public static void MapContributorEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/teams/{teamSlug}/events/{eventSlug}/contributors")
            .WithTags("Contributors")
            .AddEndpointFilter<ValidationFilter>()
            .AddEndpointFilter<UnitOfWorkFilter>()
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status500InternalServerError);

        group
            .MapAddContributor()
            .MapGetContributors()
            .MapRemoveContributor()
            .MapUpdateContributor();
    }
}
