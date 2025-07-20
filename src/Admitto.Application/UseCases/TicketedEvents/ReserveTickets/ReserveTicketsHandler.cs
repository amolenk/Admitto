using Amolenk.Admitto.Application.Common.Validation;
using Amolenk.Admitto.Domain.ValueObjects;

namespace Amolenk.Admitto.Application.UseCases.TicketedEvents.ReserveTickets;

/// <summary>
/// Reserves the required tickets for a registration.
/// </summary>
public class ReserveTicketsHandler(IDomainContext context, IUnitOfWork unitOfWork) 
    : ICommandHandler<ReserveTicketsCommand>
{
    public async ValueTask HandleAsync(ReserveTicketsCommand command, CancellationToken cancellationToken)
    {
        var ticketedEvent = await context.TicketedEvents.FindAsync([command.TicketedEventId], cancellationToken);
        if (ticketedEvent is null)
        {
            throw ValidationError.TicketedEvent.NotFound(command.TicketedEventId);
        }

        var ignoreMaxCapacity = command.RegistrationType == RegistrationType.Internal;
        
        ticketedEvent.TryReserveTickets(command.RegistrationId, command.Tickets, ignoreMaxCapacity);
    }
}