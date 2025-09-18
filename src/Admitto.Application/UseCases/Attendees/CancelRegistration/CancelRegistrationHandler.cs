using Amolenk.Admitto.Application.Common;

namespace Amolenk.Admitto.Application.UseCases.Attendees.CancelRegistration;

/// <summary>
/// Cancels the registration process for an attendee.
/// </summary>
public class CancelRegistrationHandler(IApplicationContext context)
    : ICommandHandler<CancelRegistrationCommand>
{
    public async ValueTask HandleAsync(CancelRegistrationCommand command, CancellationToken cancellationToken)
    {
        var ticketedEvent = await context.TicketedEvents.FindAsync([command.TicketedEventId], cancellationToken);
        if (ticketedEvent is null)
        {
            throw new ApplicationRuleException(ApplicationRuleError.TicketedEvent.NotFound);
        }
        
        var attendee = await context.Attendees.FindAsync([command.AttendeeId], cancellationToken);

        // TODO Double check this everywhere. We need to make sure that underlying entities belong to the correct parent.
        if (attendee is null || attendee.TicketedEventId != command.TicketedEventId)
        {
            throw new ApplicationRuleException(ApplicationRuleError.Attendee.NotFound);
        }
        
        attendee.CancelRegistration(ticketedEvent.CancellationPolicy, ticketedEvent.StartsAt);

        context.Attendees.Remove(attendee);
    }
}