using Amolenk.Admitto.Core.Registrations.Domain.DomainEvents;
using Amolenk.Admitto.Core.Registrations.Contracts.ValueObjects;
using Amolenk.Admitto.Core.Registrations.Domain.ValueObjects;
using Amolenk.Admitto.Core.Shared.Application.Messaging;

namespace Amolenk.Admitto.Core.Registrations.Application.UseCases.Registrations.WriteActivityLog.EventHandlers;

internal sealed class RegistrationCancelledDomainEventHandler(WriteActivityLogHandler handler)
    : IDomainEventHandler<RegistrationCancelledDomainEvent>
{
    public async ValueTask HandleAsync(
        RegistrationCancelledDomainEvent domainEvent,
        CancellationToken cancellationToken)
    {
        await handler.HandleAsync(
            new WriteActivityLogCommand(
                domainEvent.RegistrationId,
                ActivityType.Cancelled,
                domainEvent.OccurredOn,
                Metadata: domainEvent.Reason.ToString()),
            cancellationToken);
    }
}
