using Amolenk.Admitto.Module.Registrations.Domain.DomainEvents;
using Amolenk.Admitto.Module.Registrations.Domain.ValueObjects;
using Amolenk.Admitto.Module.Shared.Application.Messaging;

namespace Amolenk.Admitto.Module.Registrations.Application.UseCases.Registrations.WriteActivityLog.EventHandlers;

internal sealed class RegistrationCancelledDomainEventHandler(IMediator mediator)
    : IDomainEventHandler<RegistrationCancelledDomainEvent>
{
    public async ValueTask HandleAsync(
        RegistrationCancelledDomainEvent domainEvent,
        CancellationToken cancellationToken)
    {
        await mediator.SendAsync(
            new WriteActivityLogCommand(
                domainEvent.RegistrationId,
                ActivityType.Cancelled,
                domainEvent.OccurredOn,
                Metadata: domainEvent.Reason.ToString()),
            cancellationToken);
    }
}
