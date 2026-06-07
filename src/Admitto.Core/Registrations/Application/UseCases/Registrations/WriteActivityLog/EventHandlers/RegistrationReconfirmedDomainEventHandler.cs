using Amolenk.Admitto.Core.Registrations.Domain.DomainEvents;
using Amolenk.Admitto.Core.Registrations.Domain.ValueObjects;
using Amolenk.Admitto.Core.Shared.Application.Messaging;

namespace Amolenk.Admitto.Core.Registrations.Application.UseCases.Registrations.WriteActivityLog.EventHandlers;

internal sealed class RegistrationReconfirmedDomainEventHandler(ICommandHandler<WriteActivityLogCommand> handler)
    : IDomainEventHandler<RegistrationReconfirmedDomainEvent>
{
    public async ValueTask HandleAsync(
        RegistrationReconfirmedDomainEvent domainEvent,
        CancellationToken cancellationToken)
    {
        await handler.HandleAsync(
            new WriteActivityLogCommand(
                domainEvent.TeamId,
                domainEvent.TicketedEventId,
                domainEvent.RegistrationId,
                ActivityType.Reconfirmed,
                domainEvent.ReconfirmedAt),
            cancellationToken);
    }
}
