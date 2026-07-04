using Amolenk.Admitto.Core.Registrations.Application.Persistence;
using Amolenk.Admitto.Core.Registrations.Domain.DomainEvents;
using Amolenk.Admitto.Core.Registrations.Domain.ValueObjects;
using Amolenk.Admitto.Core.Shared.Application.Messaging;

namespace Amolenk.Admitto.Core.Registrations.Application.UseCases.TicketedEvents.ProjectEventStatus.EventHandlers;

/// <summary>
/// Projects a <see cref="TicketedEventStatusChangedDomainEvent"/> onto the owning event's
/// <c>TicketCatalog</c> in the *same* unit of work as the <c>TicketedEvent</c> lifecycle change,
/// so that a concurrent registration cannot slip past the atomic capacity claim after archive.
/// </summary>
internal sealed class TicketedEventStatusChangedDomainEventHandler(IRegistrationsWriteStore writeStore)
    : IDomainEventHandler<TicketedEventStatusChangedDomainEvent>
{
    public async ValueTask HandleAsync(
        TicketedEventStatusChangedDomainEvent domainEvent,
        CancellationToken cancellationToken)
    {
        var catalog = await writeStore.TicketCatalogs
            .FirstOrDefaultAsync(
                tc => tc.Id == domainEvent.TicketedEventId && tc.TeamId == domainEvent.TeamId,
                cancellationToken);

        if (catalog is null) return;

        if (domainEvent.NewStatus == EventLifecycleStatus.Archived)
            catalog.MarkEventArchived();
    }
}
