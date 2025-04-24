using Amolenk.Admitto.Application.Common;
using Amolenk.Admitto.Domain.Entities;

namespace Amolenk.Admitto.Application.UseCases.TicketedEvents.CreateTicketedEvent;

/// <summary>
/// Create a new ticketed event.
/// </summary>
public static class CreateTicketedEventEndpoint
{
    public static RouteGroupBuilder MapCreateTicketedEvent(this RouteGroupBuilder group)
    {
        group.MapPost("/", CreateTicketedEvent);
        return group;
    }

    private static async ValueTask<Results<Created<CreateTicketedEventResponse>, BadRequest<string>>> CreateTicketedEvent(
        Guid teamId, CreateTicketedEventRequest request, CreateTicketedEventValidator validator, IDomainContext context,
        CancellationToken cancellationToken)
    {
        await validator.ValidateAndThrowAsync(request, cancellationToken);
        
        var team = await context.Teams.FindAsync([teamId], cancellationToken);
        if (team is null)
        {
            return TypedResults.BadRequest(Error.TeamNotFound(teamId));
        }
        
        var newEvent = CreateTicketedEventFromRequest(request);

        team.AddActiveEvent(newEvent);

        var response = CreateTicketedEventResponse.FromTicketedEvent(newEvent);
        
        return TypedResults.Created($"/teams/{teamId}/events/{newEvent.Id}", response);
    }

    private static TicketedEvent CreateTicketedEventFromRequest(CreateTicketedEventRequest request)
    {
        var newEvent = TicketedEvent.Create(request.Name, request.StartDateTime, request.EndDateTime,
            request.RegistrationStartDateTime, request.RegistrationEndDateTime);

        foreach (var ticketTypeDto in request.TicketTypes ?? [])
        {
            var ticketType = TicketType.Create(ticketTypeDto.Name, ticketTypeDto.SlotName, ticketTypeDto.MaxCapacity);
            
            newEvent.AddTicketType(ticketType);
        }

        return newEvent;
    }
}