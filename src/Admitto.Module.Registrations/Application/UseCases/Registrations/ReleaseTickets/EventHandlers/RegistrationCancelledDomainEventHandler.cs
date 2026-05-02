using Amolenk.Admitto.Module.Registrations.Domain.DomainEvents;
using Amolenk.Admitto.Module.Shared.Application.Messaging;

namespace Amolenk.Admitto.Module.Registrations.Application.UseCases.Registrations.ReleaseTickets.EventHandlers;

internal sealed class RegistrationCancelledDomainEventHandler(IMediator mediator)
    : IDomainEventHandler<RegistrationCancelledDomainEvent>
{
    public async ValueTask HandleAsync(
        RegistrationCancelledDomainEvent domainEvent,
        CancellationToken cancellationToken)
    {
        await mediator.SendAsync(
            new ReleaseTicketsCommand(
                domainEvent.RegistrationId,
                domainEvent.TicketedEventId),
            cancellationToken);
    }
}
