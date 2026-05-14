using System.Text.Json;
using Amolenk.Admitto.Core.Registrations.Domain.DomainEvents;
using Amolenk.Admitto.Core.Registrations.Contracts.ValueObjects;
using Amolenk.Admitto.Core.Registrations.Domain.ValueObjects;
using Amolenk.Admitto.Core.Shared.Application.Messaging;

namespace Amolenk.Admitto.Core.Registrations.Application.UseCases.Registrations.WriteActivityLog.EventHandlers;

internal sealed class TicketsChangedDomainEventHandler(WriteActivityLogHandler handler)
    : IDomainEventHandler<TicketsChangedDomainEvent>
{
    public async ValueTask HandleAsync(
        TicketsChangedDomainEvent domainEvent,
        CancellationToken cancellationToken)
    {
        var metadata = JsonSerializer.Serialize(new
        {
            from = domainEvent.OldTickets.Select(t => t.Slug.Value).ToArray(),
            to = domainEvent.NewTickets.Select(t => t.Slug.Value).ToArray()
        });

        await handler.HandleAsync(
            new WriteActivityLogCommand(
                domainEvent.RegistrationId,
                ActivityType.TicketsChanged,
                domainEvent.ChangedAt,
                Metadata: metadata),
            cancellationToken);
    }
}
