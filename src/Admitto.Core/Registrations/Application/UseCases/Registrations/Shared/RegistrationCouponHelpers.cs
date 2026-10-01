using Amolenk.Admitto.Core.Registrations.Application.Persistence;
using Amolenk.Admitto.Core.Registrations.Domain.Entities;
using Amolenk.Admitto.Core.Registrations.Domain.ValueObjects;

namespace Amolenk.Admitto.Core.Registrations.Application.UseCases.Registrations.Shared;

internal static class RegistrationCouponHelpers
{
    public static (
        IReadOnlyList<TicketTypeId> CouponGrantedIds,
        IReadOnlyList<TicketTypeId> PublicIds) SplitCouponGranted(
        Coupon? coupon,
        EmailAddress email,
        IReadOnlyList<TicketTypeId> requestedIds,
        DateTimeOffset now)
    {
        var couponGrantedIds = coupon is null
            ? []
            : coupon.Redeem(email, requestedIds, now);

        return (couponGrantedIds, requestedIds.Except(couponGrantedIds).ToList());
    }

    public static IReadOnlyList<TicketTypeSnapshot> DescribeActiveWaitlistEntries(
        IEnumerable<Waitlist> eventWaitlists,
        TicketCatalog catalog,
        EmailAddress email) =>
        catalog.DescribeTicketTypes(eventWaitlists.Where(w => w.HasActiveEntry(email)).Select(w => w.Id));

    public static async ValueTask ApplyRedemptionToWaitlistsAsync(
        IRegistrationsWriteStore writeStore,
        IEnumerable<Waitlist> eventWaitlists,
        TicketCatalog catalog,
        Coupon coupon,
        EmailAddress email,
        IReadOnlyList<TicketTypeId> couponGrantedIds,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var grantedWaitlists = eventWaitlists.Where(w => couponGrantedIds.Contains(w.Id)).ToList();

        foreach (var waitlist in grantedWaitlists)
            waitlist.ApplyCouponRedemption(coupon.Id, email, catalog);

        var otherIssuedCouponIdsByWaitlist = grantedWaitlists
            .Select(waitlist => (
                Waitlist: waitlist,
                CouponIds: waitlist.Coupons
                    .Where(c => c.Status == WaitlistCouponStatus.Issued && c.Id != coupon.Id)
                    .Select(c => c.Id)
                    .ToList()))
            .Where(x => x.CouponIds.Count > 0)
            .ToList();
        if (otherIssuedCouponIdsByWaitlist.Count == 0)
            return;

        var allOtherCouponIds = otherIssuedCouponIdsByWaitlist.SelectMany(x => x.CouponIds).ToList();
        var otherOffersById = (await writeStore.Coupons
                .Where(c => allOtherCouponIds.Contains(c.Id) && c.Email == email)
                .ToListAsync(cancellationToken))
            .ToDictionary(c => c.Id);

        foreach (var (waitlist, couponIds) in otherIssuedCouponIdsByWaitlist)
        {
            foreach (var couponId in couponIds)
            {
                if (!otherOffersById.TryGetValue(couponId, out var offer))
                    continue;

                if (waitlist.WithdrawCoupon(offer.Id, catalog))
                    offer.Expire(now);
            }
        }
    }

    public static bool WindowBypassApplies(Coupon? coupon, bool hasOtherChanges) =>
        coupon?.BypassRegistrationWindow == true && !hasOtherChanges;
}
