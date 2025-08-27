namespace Amolenk.Admitto.Application.UseCases.TicketedEvents.RemoveAttendee;

/// <summary>
/// Represents a command handler that processes the removal of an attendee from a ticketed event.
/// </summary>
public class RemoveAttendeeHandler(IApplicationContext context) : ICommandHandler<RemoveAttendeeCommand>
{
    public async ValueTask HandleAsync(RemoveAttendeeCommand command, CancellationToken cancellationToken)
    {
        var ticketedEvent = await context.TicketedEvents.GetEntityAsync(
            command.TicketedEventId,
            cancellationToken: cancellationToken);

        ticketedEvent.RemoveAttendee(command.Email, command.Tickets);
    }
}