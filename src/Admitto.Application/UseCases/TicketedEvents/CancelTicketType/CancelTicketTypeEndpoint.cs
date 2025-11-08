using Amolenk.Admitto.Application.Common;
using Amolenk.Admitto.Application.UseCases.Attendees.RemoveTickets;

namespace Amolenk.Admitto.Application.UseCases.TicketedEvents.CancelTicketType;

/// <summary> 
/// Cancels an existing ticket type.
/// </summary>
public static class CancelTicketTypeEndpoint
{
    public static RouteGroupBuilder MapCancelTicketType(this RouteGroupBuilder group)
    {
        group
            .MapDelete("/{eventSlug}/ticket-types/{slug}", CancelTicketType)
            .WithName(nameof(CancelTicketType))
            .RequireAuthorization(policy => policy.RequireCanUpdateEvent());

        return group;
    }

    private static async ValueTask<Ok> CancelTicketType(
        string teamSlug,
        string eventSlug,
        string slug,
        ISlugResolver slugResolver,
        IApplicationContext context,
        IMessageOutbox outbox,
        CancellationToken cancellationToken)
    {
        var eventId = await slugResolver.ResolveTicketedEventIdAsync(teamSlug, eventSlug, cancellationToken);

        var ticketedEvent = await context.TicketedEvents
            .FirstOrDefaultAsync(te => te.Id == eventId, cancellationToken);
        if (ticketedEvent is null)
        {
            throw new ApplicationRuleException(ApplicationRuleError.TicketedEvent.NotFound);
        }

        // First set the max capacity to 0 to prevent new registrations.
        ticketedEvent.UpdateMaxCapacity(slug, 0);
        
        // Find all attendees holding tickets of the type being cancelled.
        var ticketHolders = await context.Attendees
            .Where(a => a.TicketedEventId == eventId && a.Tickets.Any(t => t.TicketTypeSlug == slug))
            .ToListAsync(cancellationToken);

        // Remove the tickets from each attendee currently holding them.
        foreach (var attendee in ticketHolders)
        {
            outbox.Enqueue(new RemoveTicketsCommand(eventId, attendee.Id, slug));
        }
        
        return TypedResults.Ok();
    }
}