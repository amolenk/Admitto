using Amolenk.Admitto.ApiService.Middleware;
using Amolenk.Admitto.Application.UseCases.Public.Cancel;
using Amolenk.Admitto.Application.UseCases.Public.CheckIn;
using Amolenk.Admitto.Application.UseCases.Public.GetQRCode;
using Amolenk.Admitto.Application.UseCases.Public.Reconfirm;
using Amolenk.Admitto.Application.UseCases.Public.Register;
using Amolenk.Admitto.Application.UseCases.Public.RequestOtpCode;
using Amolenk.Admitto.Application.UseCases.Public.VerifyOtpCode;

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
            .MapCheckIn()
            .MapCancel()
            .MapGetQRCode()
            .MapReconfirm()
            .MapRegister()
            .MapRequestOtp()
            .MapVerifyOtpCode();
    }
}
