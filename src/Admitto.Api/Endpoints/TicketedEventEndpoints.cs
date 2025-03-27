using Amolenk.Admitto.Application.UseCases.TicketedEvents.CreateTicketedEvent;
using Amolenk.Admitto.Application.UseCases.TicketedEvents.GetTicketedEvent;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Amolenk.Admitto.ApiService.Endpoints;

public static class TicketedEventEndpoints
{
    public static void MapTicketedEventEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/events").WithTags("Events");

        group.MapGet("/{id:guid}", GetEvent)
            .WithName(nameof(GetEvent))
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound);

        group.MapPost("/", CreateEvent)
            .WithName(nameof(CreateEvent))
            .Produces(StatusCodes.Status201Created)
            .ProducesValidationProblem();
    }

    private static async Task<IResult> GetEvent(
        Guid teamId, Guid ticketedEventId, GetTicketedEventHandler handler)
    {
        var query = new GetTicketedEventQuery(teamId, ticketedEventId);
        
        var result = await handler.HandleAsync(query, CancellationToken.None);
        return result is not null ? Results.Ok(result) : Results.NotFound();
    }

    private static async Task<Results<Created<CreateTicketedEventResult>, ValidationProblem>> CreateEvent(
        CreateTicketedEventCommand command, CreateTicketedEventHandler handler)
    {
        var result = await handler.HandleAsync(command, CancellationToken.None);

        return TypedResults.Created($"/events/{result.Id}", result);
    }
}