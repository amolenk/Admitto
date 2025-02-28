using Amolenk.Admitto.Application.Exceptions;
using Amolenk.Admitto.Domain.Entities;
using Amolenk.Admitto.Domain.ValueObjects;

namespace Amolenk.Admitto.Application.Features.Attendees.RegisterAttendee;

/// <summary>
/// The registration flow optimistically adds a new registration.
/// The flow includes checks for event capacity and whether the attendee
/// already has a registration.
/// However, there's a race condition where the event fills up while we're
/// executing the registration flow. Therefore, an asynchronous ReserveTicketsCommand
/// is published to check event capacity and finalize ticket reservations.
/// </summary>
public class RegisterAttendeeHandler(ITicketedEventRepository ticketedEventRepository, IAttendeeRepository attendeeRepository)
    : ICommandHandler<RegisterAttendeeCommand>
{
    public async ValueTask HandleAsync(RegisterAttendeeCommand command, CancellationToken cancellationToken)
    {
        var ticketOrder = TicketOrder.Create(command.TicketTypes);
        
        // Early exit: If there's not enough capacity, reject immediately.
        var (ticketedEvent, _) = await ticketedEventRepository.GetByIdAsync(command.TicketedEventId);
        if (!ticketedEvent.HasAvailableCapacity(ticketOrder))
        {
            throw new InsufficientCapacityException();
        }

        // Get the Attendee aggregate (or create it if it doesn't exist yet.
        var (attendee, etag) = await attendeeRepository.GetOrAddAsync(
            Attendee.GetId(command.Email),
            () => Attendee.Create(command.Email));

        // Optimistically add registration
        var registration = attendee.RegisterForEvent(command.TicketedEventId, ticketOrder);
        
        // Add a command to the outbox to reserve the tickets asynchronously.
        // At this point, everything looks ok, but we can't be 100% sure the event isn't full.
        List<OutboxMessage> outboxMessages = [
            OutboxMessage.FromCommand(
                new ReserveTicketsCommand(registration.Id, attendee.Id, command.TicketedEventId, ticketOrder)),
            ..attendee.GetDomainEvents().Select(OutboxMessage.FromDomainEvent)
        ];

        await attendeeRepository.SaveChangesAsync(attendee, etag, outboxMessages);
    }
}
