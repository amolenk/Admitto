using Amolenk.Admitto.Application.Dtos;
using Amolenk.Admitto.Application.Exceptions;

namespace Amolenk.Admitto.Application.Features.Attendees.RegisterAttendee;

/// <summary>
/// Reserves the tickets required to confirm a registration.
/// We track the processed commands to guarantee exactly-once processing.
/// </summary>
public class ReserveTicketsHandler(ITicketedEventRepository ticketedTicketedEventRepository)
    : IRequestHandler<ReserveTicketsCommand>
{
    public async Task Handle(ReserveTicketsCommand request, CancellationToken cancellationToken)
    {
        var ticketedEventResult = await ticketedTicketedEventRepository.GetByIdAsync(request.TicketedEventId);
        if (ticketedEventResult is null) throw new TicketedEventNotFoundException("Event not found");
        
        var ticketsReserved = ticketedEventResult.Aggregate.TryReserveTickets(request.TicketOrder);

        // Add commands to outbox to proceed with the registration process.
        List<OutboxMessage> outboxMessages = [];
        if (ticketsReserved)
        {
            // Tickets reserved, confirm the registration.
            outboxMessages.Add(OutboxMessage.FromCommand(
                new FinalizeRegistrationCommand(request.RegistrationId, request.AttendeeId, request.TicketedEventId)));
        }
        else
        {
            // No tickets claimed, reject the registration.
            outboxMessages.Add(OutboxMessage.FromCommand(
                new RejectRegistrationCommand(request.RegistrationId, request.AttendeeId, request.TicketedEventId)));
        }
        
        await ticketedTicketedEventRepository.SaveChangesAsync(
            ticketedEventResult.Aggregate,
            ticketedEventResult.Etag,
            outboxMessages,
            request);
    }
}
