using Amolenk.Admitto.Module.Registrations.Domain.DomainEvents;
using Amolenk.Admitto.Module.Registrations.Domain.ValueObjects;
using Amolenk.Admitto.Module.Shared.Application.Messaging;

namespace Amolenk.Admitto.Module.Registrations.Application.UseCases.Registrations.WriteActivityLog.EventHandlers;

internal sealed class AttendeeRegisteredDomainEventHandler(IMediator mediator)
    : IDomainEventHandler<AttendeeRegisteredDomainEvent>
{
    public async ValueTask HandleAsync(
        AttendeeRegisteredDomainEvent domainEvent,
        CancellationToken cancellationToken)
    {
        await mediator.SendAsync(
            new WriteActivityLogCommand(
                domainEvent.RegistrationId,
                ActivityType.Registered,
                domainEvent.OccurredOn),
            cancellationToken);
    }
}
