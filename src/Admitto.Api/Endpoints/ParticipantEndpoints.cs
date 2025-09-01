using Amolenk.Admitto.ApiService.Middleware;
using Amolenk.Admitto.Application.UseCases.Participants.AdmitParticipant;
using Amolenk.Admitto.Application.UseCases.Participants.GetQRCode;

namespace Amolenk.Admitto.ApiService.Endpoints;

public static class ParticipantEndpoints
{
    public static void MapParticipantEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/teams/{teamSlug}/events/{eventSlug}/participants")
            .WithTags("Participants")
            .AddEndpointFilter<ValidationFilter>()
            .AddEndpointFilter<UnitOfWorkFilter>()
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status500InternalServerError)
            .RequireAuthorization();

        group
            .MapAdmitParticipant()
            .MapGetQRCode();
    }
}
