using Amolenk.Admitto.Application.Common;
using ValidationException = System.ComponentModel.DataAnnotations.ValidationException;

namespace Amolenk.Admitto.Application.UseCases.Attendees.RegisterAttendee;

/// <summary>
/// Reserves the tickets required to confirm a registration.
/// We track the processed commands to guarantee exactly-once processing.
/// </summary>
public class ReserveTicketsHandler(IDomainContext context, IMessageOutbox messageOutbox)
    : ICommandHandler<ReserveTicketsCommand>
{
    public async ValueTask HandleAsync(ReserveTicketsCommand command, CancellationToken cancellationToken)
    {
        var registration = await context.AttendeeRegistrations.FindAsync([command.RegistrationId], cancellationToken);
        if (registration is null)
        {
            throw new ValidationException(Error.AttendeeRegistrationNotFound(command.RegistrationId));
        }
        
        var team = await context.Teams.FindAsync([registration.TeamId], cancellationToken);
        if (team is null)
        {
            throw new ValidationException(Error.TeamNotFound(registration.TeamId));
        }
        
        var ticketedEvent = team.ActiveEvents.FirstOrDefault(e => e.Id == registration.TicketedEventId.Value);
        if (ticketedEvent is null)
        {
            throw new ValidationException(Error.TicketedEventNotFound(registration.TicketedEventId));
        }
        
        // Try to reserve the required tickets for the event.
        var succes = ticketedEvent.TryReserveTickets(registration.TicketOrder);
        
        context.Teams.Update(team);
        
        // Also add a command to the outbox to resolve the pending registration.
        messageOutbox.Enqueue(new ResolvePendingRegistrationCommand(command.RegistrationId, succes));

        // TODO Use some kind of inbox pattern to ensure exactly-once processing
    }
}
