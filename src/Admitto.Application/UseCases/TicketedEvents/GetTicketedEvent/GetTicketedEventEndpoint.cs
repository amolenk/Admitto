using Amolenk.Admitto.Application.Common;

namespace Amolenk.Admitto.Application.UseCases.TicketedEvents.GetTicketedEvent;

/// <summary>
/// Get a specific ticketed event (including availability and ticket types).
/// </summary>
public static class GetTicketedEventEndpoint
{
    public static RouteGroupBuilder MapGetTicketedEvent(this RouteGroupBuilder group)
    {
        group
            .MapGet("/{eventSlug}", GetTicketedEvent)
            .WithName(nameof(GetTicketedEvent))
            .RequireAuthorization(policy => policy.RequireCanViewEvent());

        return group;
    }

    private static async ValueTask<Ok<GetTicketedEventResponse>> GetTicketedEvent(
        string teamSlug,
        string eventSlug,
        ISlugResolver slugResolver,
        IApplicationContext context,
        CancellationToken cancellationToken)
    {
        var eventId = await slugResolver.ResolveTicketedEventIdAsync(teamSlug, eventSlug, cancellationToken);

        var ticketedEvent = await context.TicketedEvents
            .Join(
                context.TicketedEventAvailability, // .Include(tea => tea.TicketTypes), TODO Don't need it?
                te => te.Id,
                tea => tea.TicketedEventId,
                (te, tea) => new { Event = te, Availability = tea })
            .Where(te => te.Event.Id == eventId)
            .FirstOrDefaultAsync(cancellationToken);
        
        if (ticketedEvent is null)
        {
            throw new ApplicationRuleException(ApplicationRuleError.TicketedEvent.NotFound);
        }

        var ticketTypes = ticketedEvent.Availability.TicketTypes.Select(tt => new TicketTypeDto(
            tt.Slug,
            tt.Name,
            tt.SlotName,
            tt.MaxCapacity,
            tt.UsedCapacity));

        var response = new GetTicketedEventResponse(
            ticketedEvent.Event.Slug,
            ticketedEvent.Event.Name,
            ticketedEvent.Event.StartTime,
            ticketedEvent.Event.EndTime,
            ticketedEvent.Availability.RegistrationStartTime,
            ticketedEvent.Availability.RegistrationEndTime,
            ticketedEvent.Event.BaseUrl,
            ticketTypes);

        return TypedResults.Ok(response);
    }
}