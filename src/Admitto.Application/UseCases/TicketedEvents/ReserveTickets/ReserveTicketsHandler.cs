namespace Amolenk.Admitto.Application.UseCases.TicketedEvents.ReserveTickets;

/// <summary>
/// Reserves the required tickets for a registration.
/// </summary>
public class ReserveTicketsHandler(IApplicationContext context)
    : ICommandHandler<ReserveTicketsCommand>
{
    public async ValueTask HandleAsync(ReserveTicketsCommand command, CancellationToken cancellationToken)
    {
        var ticketedEvent = await context.TicketedEvents.GetEntityAsync(
            command.TicketedEventId,
            cancellationToken: cancellationToken);

        ticketedEvent.ReserveTickets(command.AttendeeId, command.Tickets, command.IgnoreAvailability);
    }
}