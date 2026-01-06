using Amolenk.Admitto.Application.Common;
using Amolenk.Admitto.Application.Common.Messaging;
using Amolenk.Admitto.Application.Common.Persistence;

namespace Amolenk.Admitto.Application.UseCases.Attendees.ChangeTickets;

/// <summary>
/// Changes the tickets for a registered attendee.
/// </summary>
public class ChangeTicketsHandler(IApplicationContext context) : IWorkerCommandHandler<ChangeTicketsCommand>
{
    public async ValueTask HandleAsync(ChangeTicketsCommand command, CancellationToken cancellationToken)
    {
        // Get the attendee and validate it belongs to the correct event.
        var attendee = await context.Attendees.FindAsync([command.AttendeeId], cancellationToken);
        if (attendee is null || attendee.TicketedEventId != command.TicketedEventId)
        {
            throw new ApplicationRuleException(ApplicationRuleError.Attendee.NotFound);
        }
        
        // Get the ticketed event.
        var ticketedEvent = await context.TicketedEvents.FindAsync([command.TicketedEventId], cancellationToken);
        if (ticketedEvent is null)
        {
            throw new ApplicationRuleException(ApplicationRuleError.TicketedEvent.NotFound);
        }

        // Release the previously claimed tickets.
        ticketedEvent.ReleaseTickets(attendee.Tickets);
        
        // Try to claim the new tickets. This can fail if there are not enough tickets available.
        ticketedEvent.ClaimTickets(
            attendee.Email,
            DateTimeOffset.UtcNow,
            command.RequestedTickets,
            ignoreCapacity: command.AdminOnBehalfOf);
        
        // Update the attendee's tickets.
        attendee.UpdateTickets(command.RequestedTickets);
    }
}