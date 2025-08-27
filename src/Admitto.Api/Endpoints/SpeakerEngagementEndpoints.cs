using Amolenk.Admitto.ApiService.Middleware;
using Amolenk.Admitto.Application.UseCases.Speakers.AddSpeakerEngagement;

namespace Amolenk.Admitto.ApiService.Endpoints;

public static class SpeakerEngagementEndpoints
{
    public static void MapSpeakerEngagementEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/teams/{teamSlug}/events/{eventSlug}/speaker-engagements")
            .WithTags("Speaker Engagements")
            .AddEndpointFilter<ValidationFilter>()
            .AddEndpointFilter<UnitOfWorkFilter>()
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status500InternalServerError);

        group
            .MapAddSpeakerEngagement();
    }
}
