using Amolenk.Admitto.Application.UseCases.Attendees.RegisterAttendee;

namespace Amolenk.Admitto.ApiService.Endpoints;

public static class AttendeeRegistrationEndpoints
{
    public static void MapAttendeeRegistrationEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/registrations").WithTags("Registrations");

        group.MapRegisterAttendee();
    }
}
