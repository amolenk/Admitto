using Amolenk.Admitto.Application.Features.Attendees.RegisterAttendee;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Amolenk.Admitto.ApiService.Endpoints;

public static class AttendeeRegistrationEndpoints
{
    public static void MapAttendeeRegistrationEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/registrations").WithTags("Registrations");

        group.MapGet("/", () => Results.Ok());
        
        group.MapPost("/", RegisterAttendee)
            .WithName(nameof(RegisterAttendee))
            .Produces(StatusCodes.Status201Created)
            .ProducesValidationProblem();
    }

    private static async Task<Results<Created, ValidationProblem>> RegisterAttendee(
        RegisterAttendeeCommand command, RegisterAttendeeHandler handler)
    {
        await handler.HandleAsync(command, CancellationToken.None);

        return TypedResults.Created();
    }
}
