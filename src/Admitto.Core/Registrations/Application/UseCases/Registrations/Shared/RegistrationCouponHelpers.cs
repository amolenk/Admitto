using Amolenk.Admitto.Core.Registrations.Application.Persistence;
using Amolenk.Admitto.Core.Registrations.Contracts;
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

    /// <summary>
    /// Whether the given email still has a selection on any of the event's waitlists: either actively queued, or
    /// holding an outstanding, unredeemed offer. Both count as a current selection on the registration.
    /// </summary>
    public static bool HasOutstandingWaitlistSelection(IEnumerable<Waitlist> eventWaitlists, EmailAddress email) =>
        eventWaitlists.Any(w => w.HasActiveEntry(email) || w.HasOfferedEntry(email));

    /// <summary>
    /// Cancels the registration with the given reason when it is <see cref="RegistrationStatus.Waitlisted"/> (no
    /// confirmed tickets) and has no selection left on any of the event's waitlists either. Leaves a registration
    /// that still holds a queue position or an outstanding offer untouched. Returns whether it cancelled.
    /// </summary>
    public static bool CancelIfExhausted(
        Registration registration,
        IEnumerable<Waitlist> eventWaitlists,
        CancellationReason reason)
    {
        if (registration.Status != RegistrationStatus.Waitlisted)
            return false;

        if (HasOutstandingWaitlistSelection(eventWaitlists, registration.Email))
            return false;

        registration.Cancel(reason);
        return true;
    }

    /// <summary>
    /// Expires the real <see cref="Coupon"/> aggregate backing a waitlist offer that <see cref="Waitlist.RemoveEntry"/>
    /// just withdrew, if any. A no-op when <paramref name="withdrawnCouponId"/> is null (the removed entry held no
    /// outstanding offer) or the coupon no longer exists.
    /// </summary>
    public static async ValueTask ExpireWithdrawnCouponAsync(
        IRegistrationsWriteStore writeStore,
        CouponId? withdrawnCouponId,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        if (withdrawnCouponId is not { } couponId)
            return;

        var offer = await writeStore.Coupons.FirstOrDefaultAsync(c => c.Id == couponId, cancellationToken);
        offer?.Expire(now);
    }

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
