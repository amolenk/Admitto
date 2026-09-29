using Amolenk.Admitto.Core.Registrations.Application.Persistence;
using Amolenk.Admitto.Core.Registrations.Domain.ValueObjects;
using Amolenk.Admitto.Core.Shared.Application.Messaging;
using Amolenk.Admitto.Core.Shared.Application.Persistence;

namespace Amolenk.Admitto.Core.Registrations.Application.UseCases.Waitlists.DisableWaitlist;

/// <summary>
/// Applies an explicit disable of a ticket type's waitlist to its <see cref="Domain.Entities.Waitlist"/>: slots
/// freed by the same update are offered to the front of the queue first, then everyone still waiting is removed
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
        var ticketType = catalog.FindTicketType(ticketTypeId);

        var coupons = waitlist.Disable(command.FreedSlots, ticketedEvent, ticketType, timeProvider.GetUtcNow());

        await writeStore.Coupons.AddRangeAsync(coupons, cancellationToken);
    }
}
