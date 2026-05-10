using Amolenk.Admitto.Core.Registrations.Domain.DomainEvents;
using Amolenk.Admitto.Core.Registrations.Domain.ValueObjects;
using Amolenk.Admitto.Core.Shared.Application.Messaging;

namespace Amolenk.Admitto.Core.Registrations.Application.UseCases.Registrations.WriteActivityLog.EventHandlers;

internal sealed class RegistrationReconfirmedDomainEventHandler(WriteActivityLogHandler handler)
    : IDomainEventHandler<RegistrationReconfirmedDomainEvent>
{
    public async ValueTask HandleAsync(
        RegistrationReconfirmedDomainEvent domainEvent,
        CancellationToken cancellationToken)
    {
        await handler.HandleAsync(
            new WriteActivityLogCommand(
                domainEvent.RegistrationId.Value,
                ActivityType.Reconfirmed,
                domainEvent.ReconfirmedAt),
            cancellationToken);
    }
}
