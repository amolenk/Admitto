using Amolenk.Admitto.Module.Registrations.Application.Persistence;
using Amolenk.Admitto.Module.Registrations.Domain.DomainEvents;
using Amolenk.Admitto.Module.Registrations.Domain.ValueObjects;
using Amolenk.Admitto.Module.Shared.Application.Messaging;
using Microsoft.EntityFrameworkCore;

namespace Amolenk.Admitto.Module.Registrations.Application.UseCases.TicketedEvents.ProjectEventStatus.EventHandlers;

/// <summary>
/// Projects a <see cref="TicketedEventStatusChangedDomainEvent"/> onto the owning event's
/// <c>TicketCatalog</c> in the *same* unit of work as the <c>TicketedEvent</c> lifecycle change,
/// so that a concurrent registration cannot slip past the atomic capacity claim after cancel/archive.
/// </summary>
internal sealed class ProjectEventStatusToCatalogDomainEventHandler(IRegistrationsWriteStore writeStore)
    : IDomainEventHandler<TicketedEventStatusChangedDomainEvent>
{
    public async ValueTask HandleAsync(
        TicketedEventStatusChangedDomainEvent domainEvent,
        CancellationToken cancellationToken)
    {
        var catalog = await writeStore.TicketCatalogs
            .FirstOrDefaultAsync(tc => tc.Id == domainEvent.TicketedEventId, cancellationToken);

        if (catalog is null) return;

        switch (domainEvent.NewStatus)
        {
            case EventLifecycleStatus.Cancelled:
                catalog.MarkEventCancelled();
                break;
            case EventLifecycleStatus.Archived:
                catalog.MarkEventArchived();
                break;
        }
    }
}
