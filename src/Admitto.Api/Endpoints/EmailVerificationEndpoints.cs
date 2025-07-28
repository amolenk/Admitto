using Amolenk.Admitto.ApiService.Middleware;
using Amolenk.Admitto.Application.UseCases.Email.SendEmail;
using Amolenk.Admitto.Application.UseCases.Email.TestEmail;
using Amolenk.Admitto.Application.UseCases.EmailVerification.RequestOtpCode;
using Amolenk.Admitto.Application.UseCases.EmailVerification.VerifyOtpCode;

namespace Amolenk.Admitto.ApiService.Endpoints;

public static class EmailVerificationEndpoints
{
    public static void MapEmailVerificationEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/teams/{teamSlug}/events/{eventSlug}/email-verification")
            .WithTags("Email Verification")
            .AddEndpointFilter<ValidationFilter>()
            .AddEndpointFilter<UnitOfWorkFilter>()
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status500InternalServerError)
            .RequireAuthorization();

        group
            .MapRequestOtp()
            .MapVerifyOtpCode();
    }
}