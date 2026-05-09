using Amolenk.Admitto.Core.Module.Registrations.Domain.DomainEvents;
using Amolenk.Admitto.Core.Module.Registrations.Domain.ValueObjects;
using Amolenk.Admitto.Core.Shared.Application.Messaging;

namespace Amolenk.Admitto.Core.Module.Registrations.Application.UseCases.Registrations.WriteActivityLog.EventHandlers;

internal sealed class AttendeeRegisteredDomainEventHandler(IMediator mediator)
    : IDomainEventHandler<AttendeeRegisteredDomainEvent>
{
    public async ValueTask HandleAsync(
        AttendeeRegisteredDomainEvent domainEvent,
        CancellationToken cancellationToken)
    {
        await mediator.SendAsync(
            new WriteActivityLogCommand(
                domainEvent.RegistrationId.Value,
                ActivityType.Registered,
                domainEvent.OccurredOn),
            cancellationToken);
    }
}
