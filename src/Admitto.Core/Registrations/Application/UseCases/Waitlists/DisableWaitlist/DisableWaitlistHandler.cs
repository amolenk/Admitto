using Amolenk.Admitto.Core.Registrations.Application.Persistence;
using Amolenk.Admitto.Core.Registrations.Application.UseCases.Registrations.Shared;
using Amolenk.Admitto.Core.Registrations.Domain.ValueObjects;
using Amolenk.Admitto.Core.Shared.Application.Messaging;
using Amolenk.Admitto.Core.Shared.Application.Persistence;

namespace Amolenk.Admitto.Core.Registrations.Application.UseCases.Waitlists.DisableWaitlist;

/// <summary>
/// Applies an explicit disable of a ticket type's waitlist to its <see cref="Domain.Entities.Waitlist"/>: slots
/// freed by the same update are offered to the front of the queue first (unless registration has closed), then everyone still waiting is removed
/// without an email. Outstanding coupons stay valid until they are redeemed or expire. A removed registration that
/// is left with no selection on any of the event's waitlists — no active queue entry, no outstanding offer — is
/// auto-cancelled (<see cref="CancellationReason.TicketTypesRemoved"/>, no email; the organizer already knows).
/// </summary>
internal sealed class DisableWaitlistHandler(
    IRegistrationsWriteStore writeStore,
    TimeProvider timeProvider)
    : ICommandHandler<DisableWaitlistCommand>
{
    public async ValueTask HandleAsync(
        DisableWaitlistCommand command,
        CancellationToken cancellationToken)
    {
        var eventId = TicketedEventId.From(command.EventId);
        var teamId = TeamId.From(command.TeamId);
        var ticketTypeId = TicketTypeId.From(command.TicketTypeId);

        var waitlist = await writeStore.Waitlists.FirstOrDefaultAsync(
            w => w.Id == ticketTypeId && w.EventId == eventId && w.TeamId == teamId,
            cancellationToken);

        if (waitlist is null)
            return;

        var ticketedEvent = await writeStore.TicketedEvents.GetAsync(
            e => e.Id == eventId && e.TeamId == teamId,
            cancellationToken);
        var catalog = await writeStore.TicketCatalogs.GetAsync(
            c => c.Id == eventId && c.TeamId == teamId,
            cancellationToken);
        // Once registration has closed, the freed slots aren't offered to anyone.
        var utcNow = timeProvider.GetUtcNow();
        var freedSlots = ticketedEvent.HasRegistrationClosed(utcNow) ? 0 : command.FreedSlots;

        var (coupons, removedEntries) = waitlist.Disable(freedSlots, ticketedEvent, catalog, utcNow);

        await writeStore.Coupons.AddRangeAsync(coupons, cancellationToken);

        if (removedEntries.Count == 0)
            return;

        // Checked against every waitlist for the event, not just this one: a removed attendee may still hold a
        // queue position or an outstanding offer on a different ticket type's waitlist.
        var allEventWaitlists = await writeStore.Waitlists
            .Where(w => w.EventId == eventId && w.TeamId == teamId)
            .ToListAsync(cancellationToken);

        var removedRegistrationIds = removedEntries.Select(e => e.RegistrationId).Distinct().ToList();
        var affectedRegistrations = await writeStore.Registrations
            .Where(r => removedRegistrationIds.Contains(r.Id))
            .ToListAsync(cancellationToken);
        foreach (var registration in affectedRegistrations)
        {
            RegistrationCouponHelpers.CancelIfExhausted(
                registration, allEventWaitlists, CancellationReason.TicketTypesRemoved);
        }
    }
}
