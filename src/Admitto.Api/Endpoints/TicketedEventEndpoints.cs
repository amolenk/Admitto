using Amolenk.Admitto.Application.Features.TicketedEvents.CreateTicketedEvent;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Amolenk.Admitto.ApiService.Endpoints;

public static class TicketedEventEndpoints
{
    public static void MapTicketedEventEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/events").WithTags("Events");

        group.MapPost("/", CreateEvent)
            .WithName(nameof(CreateEvent))
            .Produces(StatusCodes.Status201Created)
            .ProducesValidationProblem();
    }

    private static async Task<Results<Created<CreateTicketedEventResult>, ValidationProblem>> CreateEvent(
        CreateTicketedEventCommand command, CreateTicketedEventHandler handler)
    {
        var result = await handler.HandleAsync(command, CancellationToken.None);

        return TypedResults.Created($"/events/{result.Id}", result);
    }
}