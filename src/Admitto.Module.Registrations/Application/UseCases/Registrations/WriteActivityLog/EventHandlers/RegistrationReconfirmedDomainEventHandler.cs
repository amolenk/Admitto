using Amolenk.Admitto.Module.Registrations.Domain.DomainEvents;
using Amolenk.Admitto.Module.Registrations.Domain.ValueObjects;
using Amolenk.Admitto.Module.Shared.Application.Messaging;

namespace Amolenk.Admitto.Module.Registrations.Application.UseCases.Registrations.WriteActivityLog.EventHandlers;

internal sealed class RegistrationReconfirmedDomainEventHandler(IMediator mediator)
    : IDomainEventHandler<RegistrationReconfirmedDomainEvent>
{
    public async ValueTask HandleAsync(
        RegistrationReconfirmedDomainEvent domainEvent,
        CancellationToken cancellationToken)
    {
        await mediator.SendAsync(
            new WriteActivityLogCommand(
                domainEvent.RegistrationId,
                ActivityType.Reconfirmed,
                domainEvent.ReconfirmedAt),
            cancellationToken);
    }
}
