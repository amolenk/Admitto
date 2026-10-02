using Amolenk.Admitto.Core.Registrations.Domain.ValueObjects;
using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;
using Amolenk.Admitto.Core.Registrations.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Amolenk.Admitto.Core.IntegrationTests.Registrations;

internal static class WaitlistHoldAssertions
{
    /// <summary>
    /// Asserts the invariant between the two aggregates: the public seats a ticket type holds for waitlist offers
    /// equal the number of <see cref="WaitlistCouponStatus.Issued"/> automatic coupons on its waitlist. VIP offers
    /// take no hold.
    /// </summary>
    public static async ValueTask ShouldHoldOneSeatPerIssuedAutomaticCouponAsync(
        this RegistrationsDbContext dbContext,
        TicketedEventId eventId,
        TicketTypeId ticketTypeId,
        CancellationToken cancellationToken)
    {
        var catalog = await dbContext.TicketCatalogs.AsNoTracking()
            .SingleAsync(c => c.Id == eventId, cancellationToken);
        var waitlist = await dbContext.Waitlists.AsNoTracking()
            .SingleAsync(w => w.Id == ticketTypeId, cancellationToken);

        catalog.FindTicketType(ticketTypeId).WaitlistHeldCapacity.ShouldBe(
            waitlist.Coupons.Count(c =>
                c.Status == WaitlistCouponStatus.Issued && c.Origin == WaitlistCouponOrigin.Automatic),
            "the ticket type should hold one seat per outstanding automatic waitlist offer");
    }

    /// <summary>
    /// Asserts the invariant between the two aggregates: the attendees a ticket type counts as queued equal the
    /// number of <see cref="WaitlistEntryStatus.Active"/> entries on its waitlist.
    /// </summary>
    public static async ValueTask ShouldCountEveryActiveEntryAsync(
        this RegistrationsDbContext dbContext,
        TicketedEventId eventId,
        TicketTypeId ticketTypeId,
        CancellationToken cancellationToken)
    {
        var catalog = await dbContext.TicketCatalogs.AsNoTracking()
            .SingleAsync(c => c.Id == eventId, cancellationToken);
        var waitlist = await dbContext.Waitlists.AsNoTracking()
            .SingleAsync(w => w.Id == ticketTypeId, cancellationToken);

        catalog.FindTicketType(ticketTypeId).WaitlistQueuedCount.ShouldBe(
            waitlist.ActiveEntryCount,
            "the ticket type should count every attendee actively queued on its waitlist");
    }
}
