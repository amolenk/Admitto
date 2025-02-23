using Amolenk.Admitto.Application.Dtos;
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
public class RegisterAttendeeHandler(
    ITicketedEventRepository ticketedTicketedEventRepository,
    IAttendeeRepository attendeeRepository)
    : IRequestHandler<RegisterAttendeeCommand>
{
    public async Task Handle(RegisterAttendeeCommand request, CancellationToken cancellationToken)
    {
        var ticketOrder = new TicketOrder(request.TicketTypes);
        
        // Early exit: If there's not enough capacity, reject immediately.
        if (!await HasAvailableCapacityAsync(request.TicketedEventId, ticketOrder))
        {
            throw new InsufficientCapacityException();
        }

        // Get the Attendee aggregate (or create it if it doesn't exist yet.
        var attendeeResult = await attendeeRepository.GetOrAddAsync(
            Attendee.GetId(request.Email),
            () => Attendee.Create(request.Email));

        // Optimistically add registration
        var registration = attendeeResult.Aggregate.RegisterForEvent(request.TicketedEventId, ticketOrder);
        
        // Add a command to the outbox to reserve the tickets asynchronously.
        // At this point, everything looks ok, but we can't be 100% sure the event isn't full.
        List<OutboxMessage> outboxMessages = [
            OutboxMessage.FromCommand(new ReserveTicketsCommand(
                registration.Id, attendeeResult.Aggregate.Id, request.TicketedEventId, ticketOrder)),
            ..attendeeResult.Aggregate.GetDomainEvents().Select(OutboxMessage.FromDomainEvent)
        ];

        await attendeeRepository.SaveChangesAsync(
            attendeeResult.Aggregate,
            attendeeResult.Etag,
            outboxMessages);
    }

    private async Task<bool> HasAvailableCapacityAsync(Guid eventId, TicketOrder ticketOrder)
    {
        var ticketedEventResult = await ticketedTicketedEventRepository.GetByIdAsync(eventId);
        if (ticketedEventResult is null) throw new TicketedEventNotFoundException("Event not found");

        return ticketedEventResult.Aggregate.HasAvailableCapacity(ticketOrder);
    }
}
