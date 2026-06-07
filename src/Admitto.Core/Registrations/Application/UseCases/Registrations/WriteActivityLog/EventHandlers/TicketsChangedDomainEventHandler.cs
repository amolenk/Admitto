using System.Text.Json;
using Amolenk.Admitto.Core.Registrations.Domain.DomainEvents;
using Amolenk.Admitto.Core.Registrations.Domain.ValueObjects;
using Amolenk.Admitto.Core.Shared.Application.Messaging;

namespace Amolenk.Admitto.Core.Registrations.Application.UseCases.Registrations.WriteActivityLog.EventHandlers;

internal sealed class TicketsChangedDomainEventHandler(ICommandHandler<WriteActivityLogCommand> handler)
    : IDomainEventHandler<TicketsChangedDomainEvent>
{
    public async ValueTask HandleAsync(
        TicketsChangedDomainEvent domainEvent,
        CancellationToken cancellationToken)
    {
        var metadata = JsonSerializer.Serialize(new
        {
            from = domainEvent.OldTickets.Select(t => t.Id.Value).ToArray(),
            to = domainEvent.NewTickets.Select(t => t.Id.Value).ToArray()
        });

        await handler.HandleAsync(
            new WriteActivityLogCommand(
                domainEvent.TeamId,
                domainEvent.TicketedEventId,
                domainEvent.RegistrationId,
                ActivityType.TicketsChanged,
                domainEvent.ChangedAt,
                Metadata: metadata),
            cancellationToken);
    }
}
