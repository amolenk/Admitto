using Amolenk.Admitto.Application.Common;

namespace Amolenk.Admitto.Application.UseCases.Attendees.ReconfirmRegistration;

/// <summary>
/// Reconfirms the registration process for an attendee.
/// </summary>
public class ReconfirmRegistrationHandler(IApplicationContext context)
    : ICommandHandler<ReconfirmRegistrationCommand>
{
    public async ValueTask HandleAsync(ReconfirmRegistrationCommand command, CancellationToken cancellationToken)
    {
        // TODO TicketedEvent requires NoTracking; OR: just get the attendee and check if the event is correct.
        
        var ticketedEvent = await context.TicketedEvents.FindAsync([command.TicketedEventId], cancellationToken);
        if (ticketedEvent is null)
        {
            throw new ApplicationRuleException(ApplicationRuleError.TicketedEvent.NotFound);
        }
        
        var attendee = await context.Attendees.FindAsync([command.AttendeeId], cancellationToken);
        if (attendee is null)
        {
            throw new ApplicationRuleException(ApplicationRuleError.Attendee.NotFound);
        }
        
        attendee.ReconfirmRegistration();
    }
}