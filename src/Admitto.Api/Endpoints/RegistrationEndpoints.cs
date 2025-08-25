using Amolenk.Admitto.ApiService.Middleware;
using Amolenk.Admitto.Application.UseCases.Registrations.GetQRCode;
using Amolenk.Admitto.Application.UseCases.Registrations.GetRegistration;
using Amolenk.Admitto.Application.UseCases.Registrations.GetRegistrations;
using Amolenk.Admitto.Application.UseCases.Registrations.Invite;
using Amolenk.Admitto.Application.UseCases.Registrations.Register;

namespace Amolenk.Admitto.ApiService.Endpoints;

public static class RegistrationEndpoints
{
    public static void MapPendingRegistrationEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/teams/{teamSlug}/events/{eventSlug}/registrations")
            .WithTags("Attendees")
            .AddEndpointFilter<ValidationFilter>()
            .AddEndpointFilter<UnitOfWorkFilter>()
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status500InternalServerError);

        group
            .MapGetRegistration()
            .MapGetRegistrations()
            .MapInvite()
            .MapGetQRCode()
            .MapRegister();
    }
}
