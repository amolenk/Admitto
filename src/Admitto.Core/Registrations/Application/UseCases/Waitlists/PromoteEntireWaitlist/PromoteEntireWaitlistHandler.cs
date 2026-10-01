using Amolenk.Admitto.Core.Registrations.Application.Persistence;
using Amolenk.Admitto.Core.Registrations.Application.UseCases.Waitlists;
using Amolenk.Admitto.Core.Registrations.Domain.ValueObjects;
using Amolenk.Admitto.Core.Shared.Application.Messaging;
using Amolenk.Admitto.Core.Shared.Application.Persistence;

namespace Amolenk.Admitto.Core.Registrations.Application.UseCases.Waitlists.PromoteEntireWaitlist;

/// <summary>
/// Issues a waitlist coupon (and waitlist-offer email) to every active entry once the ticket type's capacity limit
/// has been removed. Unlike <see cref="ProcessWaitlistNotifications.ProcessWaitlistNotificationsHandler"/> this does
/// not require the ticket type to still be in WaitlistMode: removing the limit switches the waitlist off in the same
/// update, and every entry that was waiting must still receive an offer. Once registration has closed nobody is
/// offered and everyone stays queued; reopening the registration window promotes them then
/// (<see cref="NotifyWaitlist.EventHandlers.TicketedEventRegistrationWindowExtendedDomainEventHandler"/>).
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
        var utcNow = timeProvider.GetUtcNow();
        if (ticketedEvent.HasRegistrationClosed(utcNow))
            return;

        var catalog = await writeStore.TicketCatalogs.GetAsync(
            c => c.Id == eventId && c.TeamId == teamId,
            cancellationToken);

        var activeEmails = waitlist.Entries
            .Where(e => e.Status == WaitlistEntryStatus.Active)
            .Select(e => e.Email)
            .ToList();
        var resolveRegistrationId = await WaitlistRegistrationIdResolver.BuildAsync(
            writeStore, eventId, teamId, activeEmails, cancellationToken);

        var coupons = waitlist.IssueCouponsToAllEntries(ticketedEvent, catalog, utcNow, resolveRegistrationId);

        await writeStore.Coupons.AddRangeAsync(coupons, cancellationToken);
    }
}
