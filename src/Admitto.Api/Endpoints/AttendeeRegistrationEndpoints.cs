using Amolenk.Admitto.Application.UseCases.Attendees.RegisterAttendee;
using Amolenk.Admitto.Application.UseCases.Registrations.StartRegistration;

namespace Amolenk.Admitto.ApiService.Endpoints;

public static class AttendeeRegistrationEndpoints
{
    public static void MapAttendeeRegistrationEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/registrations/v1").WithTags("Registrations");

        group.MapRegisterAttendee();
    }
}
