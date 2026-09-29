using Amolenk.Admitto.Core.Registrations.Application.Persistence;
using Amolenk.Admitto.Core.Registrations.Domain.DomainEvents;
using Amolenk.Admitto.Core.Registrations.Domain.ValueObjects;
using Amolenk.Admitto.Core.Shared.Application.Messaging;

namespace Amolenk.Admitto.Core.Registrations.Application.UseCases.Waitlists.DeactivateWaitlist.EventHandlers;

/// <summary>
/// Clears WaitlistMode on the TicketCatalog when the Waitlist is exhausted (no active entries, no issued coupons),
/// whatever the ticket type's availability. The catalog checks its own counts
/// (<see cref="Domain.Entities.TicketType.WaitlistQueuedCount"/>, <see cref="Domain.Entities.TicketType.WaitlistHeldCapacity"/>)
/// rather than trusting the event, so a waitlist join committing concurrently on the same ticket type makes one of the
/// two saves fail.
/// </summary>
internal sealed class WaitlistExhaustedDomainEventHandler(IRegistrationsWriteStore writeStore)
    : IDomainEventHandler<WaitlistExhaustedDomainEvent>
{
    public async ValueTask HandleAsync(
        WaitlistExhaustedDomainEvent domainEvent,
        CancellationToken cancellationToken)
    {
        var catalog = await writeStore.TicketCatalogs
            .FirstOrDefaultAsync(
                tc => tc.Id == domainEvent.TicketedEventId && tc.TeamId == domainEvent.TeamId,
                cancellationToken);

        if (catalog is null)
            return;

        if (catalog.EventStatus != EventLifecycleStatus.Active)
            return;

        catalog.LiftWaitlistModeWhenExhausted(domainEvent.TicketTypeId);
    }
}
