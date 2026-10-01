using Amolenk.Admitto.Core.Registrations.Application.Persistence;
using Amolenk.Admitto.Core.Registrations.Domain.Entities;
using Amolenk.Admitto.Core.Registrations.Domain.ValueObjects;
using Amolenk.Admitto.Core.Shared.Application.Messaging;
using Amolenk.Admitto.Core.Shared.Application.Persistence;

namespace Amolenk.Admitto.Core.Registrations.Application.UseCases.Waitlists.ProcessWaitlistNotifications;

/// <summary>
/// Issues waitlist coupons to the top-ranked attendees while the ticket type has seats available. How many is decided
/// by the <see cref="TicketCatalog"/> from real capacity (<see cref="TicketType.AvailableCapacity"/>), not by the
/// change that triggered this: each automatic coupon holds a public seat, so offers stop once the seats are covered,
/// and a shortfall left by lowering <c>PublicCapacity</c> below what's committed is made up first. Each coupon raises
/// <see cref="Domain.DomainEvents.WaitlistCouponIssuedDomainEvent"/> so the email notification is published via the
/// outbox. Finally re-evaluates WaitlistMode.
/// </summary>
/// <remarks>
/// No offers go out once registration has closed (<see cref="TicketedEvent.HasRegistrationClosed"/>), whatever freed
/// the seat; the queue is left as it is. Offers issued before close stay valid until they expire. Moving the closing
/// time later re-runs this check (<see cref="Domain.DomainEvents.TicketedEventRegistrationWindowExtendedDomainEvent"/>).
/// The decision reads the event but writes the catalog, so a window reopened at the same moment as a cancellation
/// can leave that seat unoffered until the next waitlist trigger (arc42 §11).
/// </remarks>
internal sealed class ProcessWaitlistNotificationsHandler(
    IRegistrationsWriteStore writeStore,
    TimeProvider timeProvider)
    : ICommandHandler<ProcessWaitlistNotificationsCommand>
{
    public async ValueTask HandleAsync(
        ProcessWaitlistNotificationsCommand command,
        CancellationToken cancellationToken)
    {
        var eventId = TicketedEventId.From(command.EventId);
        var teamId = TeamId.From(command.TeamId);
        var ticketTypeId = TicketTypeId.From(command.TicketTypeId);

        var ticketedEvent = await writeStore.TicketedEvents.GetAsync(
            e => e.Id == eventId && e.TeamId == teamId,
            cancellationToken);
        var catalog = await writeStore.TicketCatalogs.GetAsync(
            c => c.Id == eventId && c.TeamId == teamId,
            cancellationToken);

        if (!ticketedEvent.IsActive || catalog.EventStatus != EventLifecycleStatus.Active)
            return;

        var ticketType = catalog.GetTicketType(ticketTypeId);
        if (ticketType is null || !ticketType.WaitlistEnabled || !ticketType.WaitlistMode)
            return;

        var waitlist = await writeStore.Waitlists.FirstOrDefaultAsync(
            w => w.Id == ticketTypeId && w.EventId == eventId && w.TeamId == teamId,
            cancellationToken);

        if (waitlist is null)
            return;

        var utcNow = timeProvider.GetUtcNow();
        while (!ticketedEvent.HasRegistrationClosed(utcNow)
               && ticketType.AvailableCapacity > 0
               && waitlist.IssueNextCoupon(ticketedEvent, catalog, utcNow) is { } coupon)
        {
            await writeStore.Coupons.AddAsync(coupon, cancellationToken);
        }

        catalog.ReEvaluateWaitlistMode(ticketTypeId);
    }
}
