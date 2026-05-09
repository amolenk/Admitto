using Amolenk.Admitto.Core.Module.Registrations.Domain.DomainEvents;
using Amolenk.Admitto.Core.Module.Registrations.Domain.ValueObjects;
using Amolenk.Admitto.Core.Shared.Application.Messaging;

namespace Amolenk.Admitto.Core.Module.Registrations.Application.UseCases.Registrations.WriteActivityLog.EventHandlers;

internal sealed class RegistrationReconfirmedDomainEventHandler(IMediator mediator)
    : IDomainEventHandler<RegistrationReconfirmedDomainEvent>
{
    public async ValueTask HandleAsync(
        RegistrationReconfirmedDomainEvent domainEvent,
        CancellationToken cancellationToken)
    {
        await mediator.SendAsync(
            new WriteActivityLogCommand(
                domainEvent.RegistrationId.Value,
                ActivityType.Reconfirmed,
                domainEvent.ReconfirmedAt),
            cancellationToken);
    }
}
