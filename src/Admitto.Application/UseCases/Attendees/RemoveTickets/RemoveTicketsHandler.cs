using Amolenk.Admitto.Application.Common;
using Amolenk.Admitto.Application.Common.Messaging;
using Amolenk.Admitto.Application.Common.Persistence;
using Amolenk.Admitto.Domain.ValueObjects;

namespace Amolenk.Admitto.Application.UseCases.Attendees.RemoveTickets;

/// <summary>
/// Removes tickets of a specific type for a registered attendee.
/// </summary>
public class RemoveTicketsHandler(IApplicationContext context, IMessageOutbox outbox)
    : IWorkerCommandHandler<RemoveTicketsCommand>
{
    public async ValueTask HandleAsync(RemoveTicketsCommand command, CancellationToken cancellationToken)
    {
        // Get the attendee and validate it belongs to the correct event.
        var attendee = await context.Attendees.FindAsync([command.AttendeeId], cancellationToken);
        if (attendee is null || attendee.TicketedEventId != command.TicketedEventId)
        {
            throw new ApplicationRuleException(ApplicationRuleError.Attendee.NotFound);
        }

        // If the attendee has no tickets of the specified type, do nothing.
        if (attendee.RegistrationStatus == RegistrationStatus.Canceled
            || attendee.Tickets.All(t => t.TicketTypeSlug != command.TicketTypeSlug))
        {
            return;
        }

        // Get the ticketed event.
        var ticketedEvent = await context.TicketedEvents.FindAsync([command.TicketedEventId], cancellationToken);
        if (ticketedEvent is null)
        {
            throw new ApplicationRuleException(ApplicationRuleError.TicketedEvent.NotFound);
        }

        var ticketsToKeep = attendee.Tickets
            .Where(t => t.TicketTypeSlug != command.TicketTypeSlug)
            .ToList();

        // Cancel registration if no tickets will remain.
        if (ticketsToKeep.Count == 0)
        {
            // Cancellation will automatically release the tickets.
            attendee.CancelRegistration(ticketedEvent.StartsAt, CancellationReason.TicketTypeRemoved);
        }
        // Otherwise, just remove the specified tickets.
        else
        {
            // Release the previously claimed tickets.
            ticketedEvent.ReleaseTickets(
                attendee.Tickets
                    .Where(t => t.TicketTypeSlug == command.TicketTypeSlug));

            attendee.UpdateTickets(ticketsToKeep);
        }
    }
}