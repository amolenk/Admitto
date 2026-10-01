using Amolenk.Admitto.Core.Registrations.Application.Persistence;
using Amolenk.Admitto.Core.Registrations.Application.UseCases.Waitlists;
using Amolenk.Admitto.Core.Registrations.Domain.ValueObjects;
using Amolenk.Admitto.Core.Shared.Application.Messaging;
using Amolenk.Admitto.Core.Shared.Application.Persistence;

namespace Amolenk.Admitto.Core.Registrations.Application.UseCases.Waitlists.DisableWaitlist;

/// <summary>
/// Applies an explicit disable of a ticket type's waitlist to its <see cref="Domain.Entities.Waitlist"/>: slots
/// freed by the same update are offered to the front of the queue first (unless registration has closed), then everyone still waiting is removed
/// without an email. Outstanding coupons stay valid until they are redeemed or expire.
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

        var activeEmails = waitlist.Entries
            .Where(e => e.Status == WaitlistEntryStatus.Active)
            .Select(e => e.Email)
            .ToList();
        var resolveRegistrationId = await WaitlistRegistrationIdResolver.BuildAsync(
            writeStore, eventId, teamId, activeEmails, cancellationToken);

        var coupons = waitlist.Disable(freedSlots, ticketedEvent, catalog, utcNow, resolveRegistrationId);

        await writeStore.Coupons.AddRangeAsync(coupons, cancellationToken);
    }
}
