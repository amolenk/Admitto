using Amolenk.Admitto.Core.Registrations.Domain.ValueObjects;
using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;
using Amolenk.Admitto.Core.Registrations.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Amolenk.Admitto.Core.IntegrationTests.Registrations;

internal static class WaitlistHoldAssertions
{
    /// <summary>
    /// Asserts the invariant between the two aggregates: the seats a ticket type holds for waitlist offers equal the
    /// number of <see cref="WaitlistCouponStatus.Issued"/> coupons on its waitlist.
    /// </summary>
    public static async ValueTask ShouldHoldOneSeatPerIssuedCouponAsync(
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
            waitlist.Coupons.Count(c => c.Status == WaitlistCouponStatus.Issued),
            "the ticket type should hold one seat per outstanding waitlist offer");
    }
}
