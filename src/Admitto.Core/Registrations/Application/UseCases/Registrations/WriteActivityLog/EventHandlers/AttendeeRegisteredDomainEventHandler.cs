using Amolenk.Admitto.Core.Registrations.Domain.DomainEvents;
using Amolenk.Admitto.Core.Registrations.Domain.ValueObjects;
using Amolenk.Admitto.Core.Shared.Application.Messaging;

namespace Amolenk.Admitto.Core.Registrations.Application.UseCases.Registrations.WriteActivityLog.EventHandlers;

internal sealed class AttendeeRegisteredDomainEventHandler(ICommandHandler<WriteActivityLogCommand> handler)
    : IDomainEventHandler<AttendeeRegisteredDomainEvent>
{
    public async ValueTask HandleAsync(
        AttendeeRegisteredDomainEvent domainEvent,
        CancellationToken cancellationToken)
    {
        await handler.HandleAsync(
            new WriteActivityLogCommand(
                domainEvent.RegistrationId,
                ActivityType.Registered,
                domainEvent.OccurredOn),
            cancellationToken);
    }
}
