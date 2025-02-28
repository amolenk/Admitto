namespace Amolenk.Admitto.Application.Features.Attendees.RegisterAttendee;

/// <summary>
/// Reserves the tickets required to confirm a registration.
/// We track the processed commands to guarantee exactly-once processing.
/// </summary>
public class ReserveTicketsHandler(ITicketedEventRepository ticketedEventRepository)
    : ICommandHandler<ReserveTicketsCommand>
{
    public async ValueTask HandleAsync(ReserveTicketsCommand command, CancellationToken cancellationToken)
    {
        var (ticketedEvent, etag) = await ticketedEventRepository.GetByIdAsync(command.TicketedEventId);
        
        // Try to reserve the required tickets with the event.
        var succes = ticketedEvent.TryReserveTickets(command.TicketOrder);
        
        // Add a command to the outbox to resolve the pending registration.
        List<OutboxMessage> outboxMessages = [
            OutboxMessage.FromCommand(
                new ResolvePendingRegistrationCommand(command.RegistrationId, command.AttendeeId, succes)),
            ..ticketedEvent.GetDomainEvents().Select(OutboxMessage.FromDomainEvent)
        ];
        
        await ticketedEventRepository.SaveChangesAsync(ticketedEvent, etag, outboxMessages, command);
    }
}
