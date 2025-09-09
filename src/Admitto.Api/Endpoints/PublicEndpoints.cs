using Amolenk.Admitto.ApiService.Middleware;
using Amolenk.Admitto.Application.UseCases.EmailVerification.RequestOtpCode;
using Amolenk.Admitto.Application.UseCases.EmailVerification.VerifyOtpCode;
using Amolenk.Admitto.Application.UseCases.Participants.CancelParticipation;
using Amolenk.Admitto.Application.UseCases.Participants.CheckInParticipant;
using Amolenk.Admitto.Application.UseCases.Participants.GetQRCode;
using Amolenk.Admitto.Application.UseCases.Participants.ReconfirmParticipation;

namespace Amolenk.Admitto.ApiService.Endpoints;

public static class PublicEndpoints
{
    public static void MapPublicEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/teams/{teamSlug}/events/{eventSlug}/public")
            .WithTags("Public")
            .AddEndpointFilter<ValidationFilter>()
            .AddEndpointFilter<UnitOfWorkFilter>()
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status500InternalServerError)
            .RequireCors("AllowAll");

        group
            .MapAdmit()
            .MapCancelParticipation()
            .MapGetQRCode()
            .MapReconfirmParticipation()
            .MapRequestOtp()
            .MapVerifyOtpCode();
    }
}
