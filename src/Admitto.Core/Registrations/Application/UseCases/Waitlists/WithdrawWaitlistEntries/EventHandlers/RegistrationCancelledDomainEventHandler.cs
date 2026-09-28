using Amolenk.Admitto.Core.Registrations.Application.Persistence;
using Amolenk.Admitto.Core.Registrations.Domain.DomainEvents;
using Amolenk.Admitto.Core.Shared.Application.Messaging;

namespace Amolenk.Admitto.Core.Registrations.Application.UseCases.Waitlists.WithdrawWaitlistEntries.EventHandlers;

/// <summary>
/// Handles <see cref="RegistrationCancelledDomainEvent"/> by withdrawing the cancelled attendee
/// from every waitlist they may still be actively queued on, across all ticket types for the
/// event, so a cancelled attendee can never subsequently receive a promotion offer.
/// </summary>
internal sealed class RegistrationCancelledDomainEventHandler(IRegistrationsWriteStore writeStore)
    : IDomainEventHandler<RegistrationCancelledDomainEvent>
{
    public async ValueTask HandleAsync(
        RegistrationCancelledDomainEvent domainEvent,
        CancellationToken cancellationToken)
    {
        var waitlists = await writeStore.Waitlists
            .Where(w => w.EventId == domainEvent.TicketedEventId && w.TeamId == domainEvent.TeamId)
            .ToListAsync(cancellationToken);

        foreach (var waitlist in waitlists)
        {
            waitlist.RemoveEntry(domainEvent.Email);
        }
    }
}
