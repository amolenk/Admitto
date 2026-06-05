using Amolenk.Admitto.Core.Registrations.Application.Persistence;
using Amolenk.Admitto.Core.Registrations.Domain.DomainEvents;
using Amolenk.Admitto.Core.Shared.Application.Messaging;

namespace Amolenk.Admitto.Core.Registrations.Application.UseCases.Waitlists.DeactivateWaitlist.EventHandlers;

/// <summary>
/// Attempts to deactivate WaitlistMode on the TicketCatalog when the Waitlist is exhausted
/// (no active entries, no issued coupons). WaitlistMode is cleared only when capacity is available.
/// </summary>
internal sealed class WaitlistExhaustedDomainEventHandler(IRegistrationsWriteStore writeStore)
    : IDomainEventHandler<WaitlistExhaustedDomainEvent>
{
    public async ValueTask HandleAsync(
        WaitlistExhaustedDomainEvent domainEvent,
        CancellationToken cancellationToken)
    {
        var catalog = await writeStore.TicketCatalogs
            .FirstOrDefaultAsync(tc => tc.Id == domainEvent.TicketedEventId, cancellationToken);

        if (catalog is null)
            return;

        catalog.ForceDeactivateWaitlistMode(domainEvent.TicketTypeId);
    }
}
