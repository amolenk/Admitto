namespace Amolenk.Admitto.Application.Features.Attendees.RegisterAttendee;

/// <summary>
/// Reserves the tickets required to confirm a registration.
/// We track the processed commands to guarantee exactly-once processing.
/// </summary>
public class ReserveTicketsHandler(IAttendeeRegistrationRepository registrationRepository,
    ITicketedEventRepository eventRepository)
    : ICommandHandler<ReserveTicketsCommand>
{
    public async ValueTask HandleAsync(ReserveTicketsCommand command, CancellationToken cancellationToken)
    {
        var (registration, _) = await registrationRepository.GetByIdAsync(command.RegistrationId);
        var (ticketedEvent, etag) = await eventRepository.GetByIdAsync(registration.TicketedEventId);
        
        // Try to reserve the required tickets for the event.
        var succes = ticketedEvent.TryReserveTickets(registration.TicketOrder);
        
        // Add a command to the outbox to resolve the pending registration.
        List<OutboxMessage> outboxMessages = [
            OutboxMessage.FromCommand(
                new ResolvePendingRegistrationCommand(command.RegistrationId, succes)),
            ..ticketedEvent.GetDomainEvents().Select(OutboxMessage.FromDomainEvent)
        ];
        
        await eventRepository.SaveChangesAsync(ticketedEvent, etag, outboxMessages, command);
    }
}
