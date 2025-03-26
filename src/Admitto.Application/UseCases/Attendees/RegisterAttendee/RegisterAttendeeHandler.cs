using Amolenk.Admitto.Domain.Entities;
using Amolenk.Admitto.Domain.ValueObjects;

namespace Amolenk.Admitto.Application.UseCases.Attendees.RegisterAttendee;

/// <summary>
/// The registration flow optimistically adds a new registration.
/// The flow includes checks for event capacity and whether the attendee
/// already has a registration.
/// However, there's a race condition where the event fills up while we're
/// executing the registration flow. Therefore, an asynchronous ReserveTicketsCommand
/// is published to check event capacity and finalize ticket reservations.
/// </summary>
public class RegisterAttendeeHandler(IDomainContext context, IMessageOutbox messageOutbox)
    : ICommandHandler<RegisterAttendeeCommand>
{
    public async ValueTask HandleAsync(RegisterAttendeeCommand command, CancellationToken cancellationToken)
    {
        var ticketOrder = TicketOrder.Create(command.TicketTypes);
        
        // Early exit: If there's not enough capacity, reject immediately.
        var ticketedEvent = await context.TicketedEvents.GetByIdAsync(command.TicketedEventId, cancellationToken);
        if (!ticketedEvent.HasAvailableCapacity(ticketOrder))
        {
            throw new InsufficientCapacityException();
        }

        // Optimistically add a new registration.
        var registration = AttendeeRegistration.Create(command.TicketedEventId, command.Email,
            command.FirstName, command.LastName, command.OrganizationName, ticketOrder);
        //
        context.AttendeeRegistrations.Add(registration);
        
        // Add a command to the outbox to reserve the tickets asynchronously.
        // At this point, everything looks ok, but we can't be 100% sure the event isn't full.
        messageOutbox.EnqueueCommand(new ReserveTicketsCommand(registration.Id));
    }
}
