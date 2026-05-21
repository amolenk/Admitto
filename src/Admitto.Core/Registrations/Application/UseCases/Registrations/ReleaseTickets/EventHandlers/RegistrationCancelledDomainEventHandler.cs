using Amolenk.Admitto.Core.Registrations.Domain.DomainEvents;
using Amolenk.Admitto.Core.Shared.Application.Messaging;

namespace Amolenk.Admitto.Core.Registrations.Application.UseCases.Registrations.ReleaseTickets.EventHandlers;

internal sealed class RegistrationCancelledDomainEventHandler(ICommandHandler<ReleaseTicketsCommand> handler)
    : IDomainEventHandler<RegistrationCancelledDomainEvent>
{
    public async ValueTask HandleAsync(
        RegistrationCancelledDomainEvent domainEvent,
        CancellationToken cancellationToken)
    {
        await handler.HandleAsync(
            new ReleaseTicketsCommand(
                domainEvent.RegistrationId.Value,
                domainEvent.TicketedEventId.Value),
            cancellationToken);
    }
}
