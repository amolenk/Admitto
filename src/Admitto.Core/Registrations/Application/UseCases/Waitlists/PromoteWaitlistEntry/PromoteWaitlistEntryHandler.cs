using Amolenk.Admitto.Core.Registrations.Application.Persistence;
using Amolenk.Admitto.Core.Registrations.Domain.ValueObjects;
using Amolenk.Admitto.Core.Shared.Application.Messaging;
using Amolenk.Admitto.Core.Shared.Application.Persistence;

namespace Amolenk.Admitto.Core.Registrations.Application.UseCases.Waitlists.PromoteWaitlistEntry;

/// <summary>
/// Issues a waitlist coupon to one specific active waitlist entry, out of queue order (VIP promotion).
/// The entry leaves the queue immediately and the attendee receives the regular waitlist-offer email via
/// <see cref="Domain.DomainEvents.WaitlistCouponIssuedDomainEvent"/>. The offer takes no public hold: redeemed, it
/// is an admin ticket on top of public capacity, so it never affects the offers made to the rest of the queue.
/// </summary>
internal sealed class PromoteWaitlistEntryHandler(
    IRegistrationsWriteStore writeStore,
    TimeProvider timeProvider)
    : ICommandHandler<PromoteWaitlistEntryCommand, Guid>
{
    public async ValueTask<Guid> HandleAsync(
        PromoteWaitlistEntryCommand command,
        CancellationToken cancellationToken)
    {
        var eventId = TicketedEventId.From(command.EventId);
        var teamId = TeamId.From(command.TeamId);
        var ticketTypeId = TicketTypeId.From(command.TicketTypeId);

        var ticketedEvent = await writeStore.TicketedEvents.GetUntrackedAsync(
            e => e.Id == eventId && e.TeamId == teamId,
            cancellationToken);
        var catalog = await writeStore.TicketCatalogs.GetAsync(
            c => c.Id == eventId && c.TeamId == teamId,
            cancellationToken);

        catalog.EnsureEventActive();

        var waitlist = await writeStore.Waitlists.GetAsync(
            w => w.Id == ticketTypeId && w.EventId == eventId && w.TeamId == teamId,
            cancellationToken);

        var entryId = WaitlistEntryId.From(command.EntryId);

        var coupon = waitlist.IssueCouponToEntry(
            entryId,
            ticketedEvent,
            catalog,
            timeProvider.GetUtcNow());

        await writeStore.Coupons.AddAsync(coupon, cancellationToken);

        return coupon.Id.Value;
    }
}
