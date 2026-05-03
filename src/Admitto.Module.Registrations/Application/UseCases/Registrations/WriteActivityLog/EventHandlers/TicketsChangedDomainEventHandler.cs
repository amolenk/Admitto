using System.Text.Json;
using Amolenk.Admitto.Module.Registrations.Domain.DomainEvents;
using Amolenk.Admitto.Module.Registrations.Domain.ValueObjects;
using Amolenk.Admitto.Module.Shared.Application.Messaging;

namespace Amolenk.Admitto.Module.Registrations.Application.UseCases.Registrations.WriteActivityLog.EventHandlers;

internal sealed class TicketsChangedDomainEventHandler(IMediator mediator)
    : IDomainEventHandler<TicketsChangedDomainEvent>
{
    public async ValueTask HandleAsync(
        TicketsChangedDomainEvent domainEvent,
        CancellationToken cancellationToken)
    {
        var metadata = JsonSerializer.Serialize(new
        {
            from = domainEvent.OldTickets.Select(t => t.Slug).ToArray(),
            to = domainEvent.NewTickets.Select(t => t.Slug).ToArray()
        });

        await mediator.SendAsync(
            new WriteActivityLogCommand(
                domainEvent.RegistrationId,
                ActivityType.TicketsChanged,
                domainEvent.ChangedAt,
                Metadata: metadata),
            cancellationToken);
    }
}
