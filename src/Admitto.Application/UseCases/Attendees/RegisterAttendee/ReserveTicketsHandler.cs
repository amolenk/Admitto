using Amolenk.Admitto.Application.Common.DTOs;

namespace Amolenk.Admitto.Application.UseCases.Attendees.RegisterAttendee;

/// <summary>
/// Reserves the tickets required to confirm a registration.
/// We track the processed commands to guarantee exactly-once processing.
/// </summary>
public class ReserveTicketsHandler(IApplicationContext context) : ICommandHandler<ReserveTicketsCommand>
{
    public async ValueTask HandleAsync(ReserveTicketsCommand command, CancellationToken cancellationToken)
    {
        var registration = await context.AttendeeRegistrations.GetByIdAsync(command.RegistrationId, cancellationToken);
        var ticketedEvent = await context.TicketedEvents.GetByIdAsync(registration.TicketedEventId, cancellationToken);
        
        // Try to reserve the required tickets for the event.
        var succes = ticketedEvent.TryReserveTickets(registration.TicketOrder);
        
        context.TicketedEvents.Update(ticketedEvent);
        
        // Also add a command to the outbox to resolve the pending registration.
        context.Outbox.Add(OutboxMessageDto.FromCommand(
            new ResolvePendingRegistrationCommand(command.RegistrationId, succes)));

        // TODO Use some kind of inbox pattern to ensure exactly-once processing
        await context.SaveChangesAsync(cancellationToken);
    }
}
