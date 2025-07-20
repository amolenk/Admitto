using Amolenk.Admitto.ApiService.Middleware;
using Amolenk.Admitto.Application.UseCases.PendingRegistrations.GetPendingRegistration;
using Amolenk.Admitto.Application.UseCases.PendingRegistrations.GetPendingRegistrations;
using Amolenk.Admitto.Application.UseCases.PendingRegistrations.StartRegistration;
using Amolenk.Admitto.Application.UseCases.PendingRegistrations.VerifyRegistrationRequest;

namespace Amolenk.Admitto.ApiService.Endpoints;

public static class PendingRegistrationEndpoints
{
    public static void MapPendingRegistrationEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/teams/{teamSlug}/events/{eventSlug}/registrations")
            .WithTags("Pending Registrations")
            .AddEndpointFilter<ValidationFilter>()
            .AddEndpointFilter<UnitOfWorkFilter>()
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status500InternalServerError);

        group
            .MapGetPendingRegistration()
            .MapGetPendingRegistrations()
            .MapStartRegistration()
            .MapVerifyPendingRegistration();
    }
}
