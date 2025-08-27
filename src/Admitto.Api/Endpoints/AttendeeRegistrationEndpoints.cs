using Amolenk.Admitto.ApiService.Middleware;
using Amolenk.Admitto.Application.UseCases.AttendeeRegistrations.CancelRegistration;
using Amolenk.Admitto.Application.UseCases.AttendeeRegistrations.GetQRCode;
using Amolenk.Admitto.Application.UseCases.AttendeeRegistrations.GetRegistration;
using Amolenk.Admitto.Application.UseCases.AttendeeRegistrations.GetRegistrations;
using Amolenk.Admitto.Application.UseCases.AttendeeRegistrations.InviteAttendee;
using Amolenk.Admitto.Application.UseCases.AttendeeRegistrations.RegisterAttendee;

namespace Amolenk.Admitto.ApiService.Endpoints;

public static class AttendeeRegistrationEndpoints
{
    public static void MapAttendeeRegistrationEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/teams/{teamSlug}/events/{eventSlug}/attendee-registrations")
            .WithTags("Attendee Registrations")
            .AddEndpointFilter<ValidationFilter>()
            .AddEndpointFilter<UnitOfWorkFilter>()
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status500InternalServerError);

        group
            .MapCancelRegistration()
            .MapGetRegistration()
            .MapGetRegistrations()
            .MapGetQRCode()
            .MapInviteAttendee()
            .MapRegisterAttendee();
    }
}
