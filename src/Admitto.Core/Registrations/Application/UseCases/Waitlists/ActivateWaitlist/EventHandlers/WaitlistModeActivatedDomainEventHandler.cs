using Amolenk.Admitto.Core.Registrations.Application.Persistence;
using Amolenk.Admitto.Core.Registrations.Domain.DomainEvents;
using Amolenk.Admitto.Core.Registrations.Domain.Entities;
using Amolenk.Admitto.Core.Shared.Application.Messaging;

namespace Amolenk.Admitto.Core.Registrations.Application.UseCases.Waitlists.ActivateWaitlist.EventHandlers;

/// <summary>
/// Creates a new <see cref="Waitlist"/> aggregate when WaitlistMode is activated for a ticket type.
/// Runs synchronously inside the same EF Core transaction as the command that triggered the event.
/// </summary>
internal sealed class WaitlistModeActivatedDomainEventHandler(IRegistrationsWriteStore writeStore)
    : IDomainEventHandler<WaitlistModeActivatedDomainEvent>
{
    public async ValueTask HandleAsync(
        WaitlistModeActivatedDomainEvent domainEvent,
        CancellationToken cancellationToken)
    {
        var existing = await writeStore.Waitlists
            .AnyAsync(
                w => w.Id == domainEvent.TicketTypeId
                     && w.EventId == domainEvent.TicketedEventId
                     && w.TeamId == domainEvent.TeamId,
                cancellationToken);

        if (existing)
            return;

        var ticketedEvent = await writeStore.TicketedEvents
            .FirstOrDefaultAsync(
                e => e.Id == domainEvent.TicketedEventId && e.TeamId == domainEvent.TeamId,
                cancellationToken);

        if (ticketedEvent is null)
            return;

        var waitlist = Domain.Entities.Waitlist.Create(
            domainEvent.TicketedEventId,
            domainEvent.TicketTypeId,
            domainEvent.TeamId);

        await writeStore.Waitlists.AddAsync(waitlist, cancellationToken);
    }
}
