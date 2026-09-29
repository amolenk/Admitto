using Amolenk.Admitto.Core.Registrations.Application.Persistence;
using Amolenk.Admitto.Core.Registrations.Domain.ValueObjects;
using Amolenk.Admitto.Core.Shared.Application.Messaging;
using Amolenk.Admitto.Core.Shared.Application.Persistence;

namespace Amolenk.Admitto.Core.Registrations.Application.UseCases.Waitlists.PromoteEntireWaitlist;

/// <summary>
/// Issues a waitlist coupon (and waitlist-offer email) to every active entry once the ticket type's capacity limit
/// has been removed. Unlike <see cref="ProcessWaitlistNotifications.ProcessWaitlistNotificationsHandler"/> this does
/// not require the ticket type to still be in WaitlistMode: removing the limit switches the waitlist off in the same
/// update, and every entry that was waiting must still receive an offer.
/// </summary>
internal sealed class PromoteEntireWaitlistHandler(
    IRegistrationsWriteStore writeStore,
    TimeProvider timeProvider)
    : ICommandHandler<PromoteEntireWaitlistCommand>
{
    public async ValueTask HandleAsync(
        PromoteEntireWaitlistCommand command,
        CancellationToken cancellationToken)
    {
        var eventId = TicketedEventId.From(command.EventId);
        var teamId = TeamId.From(command.TeamId);
        var ticketTypeId = TicketTypeId.From(command.TicketTypeId);

        var waitlist = await writeStore.Waitlists.FirstOrDefaultAsync(
            w => w.Id == ticketTypeId && w.EventId == eventId && w.TeamId == teamId,
            cancellationToken);

        if (waitlist is null || waitlist.ActiveEntryCount == 0)
            return;

        var ticketedEvent = await writeStore.TicketedEvents.GetAsync(
            e => e.Id == eventId && e.TeamId == teamId,
            cancellationToken);
        var catalog = await writeStore.TicketCatalogs.GetAsync(
            c => c.Id == eventId && c.TeamId == teamId,
            cancellationToken);
        var ticketType = catalog.FindTicketType(ticketTypeId);

        var coupons = waitlist.IssueCouponsToAllEntries(ticketedEvent, ticketType, timeProvider.GetUtcNow());

        await writeStore.Coupons.AddRangeAsync(coupons, cancellationToken);
    }
}
