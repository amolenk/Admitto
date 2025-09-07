using Amolenk.Admitto.ApiService.Middleware;
using Amolenk.Admitto.Application.UseCases.Attendees.CancelRegistration;
using Amolenk.Admitto.Application.UseCases.Attendees.GetAttendee;
using Amolenk.Admitto.Application.UseCases.Attendees.GetAttendees;
using Amolenk.Admitto.Application.UseCases.Attendees.ReconfirmRegistration;
using Amolenk.Admitto.Application.UseCases.Attendees.RegisterAttendee;

namespace Amolenk.Admitto.ApiService.Endpoints;

public static class AttendeeEndpoints
{
    public static void MapAttendeeEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/teams/{teamSlug}/events/{eventSlug}/attendees")
            .WithTags("Attendees")
            .AddEndpointFilter<ValidationFilter>()
            .AddEndpointFilter<UnitOfWorkFilter>()
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status500InternalServerError)
            .RequireAuthorization();

        group
            .MapCancelRegistration()
            .MapGetAttendee()
            .MapGetAttendees()
            .MapReconfirmRegistration()
            .MapRegisterAttendee();
    }
}
