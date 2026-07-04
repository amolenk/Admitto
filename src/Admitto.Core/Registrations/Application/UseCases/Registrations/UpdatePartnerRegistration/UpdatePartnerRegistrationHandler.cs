using Amolenk.Admitto.Core.Registrations.Application.Persistence;
using Amolenk.Admitto.Core.Registrations.Contracts;
using Amolenk.Admitto.Core.Registrations.Contracts.ValueObjects;
using Amolenk.Admitto.Core.Registrations.Domain.Entities;
using Amolenk.Admitto.Core.Registrations.Domain.ValueObjects;
using Amolenk.Admitto.Core.Shared.Application.Messaging;
using Amolenk.Admitto.Core.Shared.Application.Persistence;
using Amolenk.Admitto.Core.Shared.Kernel.ErrorHandling;

namespace Amolenk.Admitto.Core.Registrations.Application.UseCases.Registrations.UpdatePartnerRegistration;

internal sealed class UpdatePartnerRegistrationHandler(
    IRegistrationsWriteStore writeStore,
    TimeProvider timeProvider)
    : ICommandHandler<UpdatePartnerRegistrationCommand>
{
    public async ValueTask HandleAsync(
        UpdatePartnerRegistrationCommand command,
        CancellationToken cancellationToken)
    {
        var eventId = TicketedEventId.From(command.EventId);
        var teamId = TeamId.From(command.TeamId);
        var registrationId = RegistrationId.From(command.RegistrationId);
        var firstName = FirstName.From(command.FirstName);
        var lastName = LastName.From(command.LastName);
        var newTicketTypeIds = command.TicketTypeIds.Select(TicketTypeId.From).ToList();

        var registration = await writeStore.Registrations.GetAsync(
            r => r.Id == registrationId && r.EventId == eventId && r.TeamId == teamId,
            cancellationToken);

        if (registration.Status == RegistrationStatus.Cancelled)
            throw new BusinessRuleViolationException(Errors.RegistrationIsCancelled);

        var ticketedEvent = await writeStore.TicketedEvents
            .GetAsync(e => e.Id == eventId && e.TeamId == teamId, cancellationToken);

        if (!ticketedEvent.IsActive)
            throw new BusinessRuleViolationException(TicketedEvent.Errors.EventNotActive);

        var now = timeProvider.GetUtcNow();
        ticketedEvent.EnsureRegistrationOpen(now);
        var additionalDetails = AdditionalDetails.Validate(
            command.AdditionalDetails,
            ticketedEvent.AdditionalDetailSchema);

        var catalog = await writeStore.TicketCatalogs
            .GetAsync(tc => tc.Id == eventId && tc.TeamId == teamId, cancellationToken);

        catalog.ValidateSelection(newTicketTypeIds);

        Coupon? waitlistCoupon = null;
        TicketTypeId? couponTicketTypeId = null;
        if (command.WaitlistCouponCode is { } waitlistCouponCode)
        {
            waitlistCoupon = await writeStore.Coupons.GetAsync(
                c => c.EventId == eventId && c.TeamId == teamId && c.Code == CouponCode.From(waitlistCouponCode),
                cancellationToken);

            if (waitlistCoupon.Source != CouponSource.Waitlist)
                throw new BusinessRuleViolationException(Errors.WaitlistCouponRequired);

            if (waitlistCoupon.AllowedTicketTypeIds.Count != 1)
                throw new BusinessRuleViolationException(Errors.WaitlistCouponRequired);

            couponTicketTypeId = waitlistCoupon.AllowedTicketTypeIds[0];
            if (!newTicketTypeIds.Contains(couponTicketTypeId.Value))
                throw new BusinessRuleViolationException(Errors.WaitlistCouponTicketMissing(couponTicketTypeId.Value));

            EnsureWaitlistCouponCanBeRedeemed(waitlistCoupon, registration.Email, couponTicketTypeId.Value, now);
        }

        var currentIds = registration.Tickets.Select(t => t.Id).ToHashSet();
        var newIds = newTicketTypeIds.ToHashSet();
        var toRelease = currentIds.Except(newIds).ToList();
        var toClaim = newTicketTypeIds.Where(id => !currentIds.Contains(id)).ToList();

        var couponBackedClaim = couponTicketTypeId is { } offeredTicketTypeId
            && toClaim.Remove(offeredTicketTypeId);

        catalog.Claim(toClaim, enforce: true);
        if (couponBackedClaim)
            catalog.Claim([couponTicketTypeId!.Value], enforce: false);
        catalog.Release(toRelease);

        var newTickets = newTicketTypeIds
            .Select(id =>
            {
                var ticketType = catalog.GetTicketType(id);
                var timeSlots = ticketType?.TimeSlots ?? [];
                var name = ticketType?.Name ?? TicketTypeName.From(id.Value.ToString());
                return new TicketTypeSnapshot(id, name, timeSlots);
            })
            .ToList();

        registration.ReplaceAttendeeEditableState(firstName, lastName, additionalDetails, newTickets, now);

        if (waitlistCoupon is null || couponTicketTypeId is null)
            return;

        waitlistCoupon.Redeem(registration.Email, [couponTicketTypeId.Value], now);

        var waitlist = await writeStore.Waitlists.GetAsync(
            w => w.EventId == eventId && w.TeamId == teamId && w.Id == couponTicketTypeId.Value,
            cancellationToken);
        waitlist.RedeemCoupon(waitlistCoupon.Id);
    }

    private static void EnsureWaitlistCouponCanBeRedeemed(
        Coupon coupon,
        EmailAddress email,
        TicketTypeId ticketTypeId,
        DateTimeOffset now)
    {
        var status = coupon.GetStatus(now);
        if (status == CouponStatus.Expired)
            throw new BusinessRuleViolationException(Coupon.Errors.Expired);
        if (status == CouponStatus.Redeemed)
            throw new BusinessRuleViolationException(Coupon.Errors.AlreadyRedeemed);
        if (status == CouponStatus.Revoked)
            throw new BusinessRuleViolationException(Coupon.Errors.Revoked);

        if (!coupon.AllowedTicketTypeIds.Contains(ticketTypeId))
            throw new BusinessRuleViolationException(Coupon.Errors.TicketTypeNotAllowlisted([ticketTypeId.Value]));

        if (coupon.Email != email)
            throw new BusinessRuleViolationException(Coupon.Errors.EmailMismatch);
    }

    internal static class Errors
    {
        public static readonly Error RegistrationIsCancelled = new(
            "registration.is_cancelled",
            "Registration is cancelled.",
            Type: ErrorType.Conflict);

        public static readonly Error WaitlistCouponRequired = new(
            "update_registration.waitlist_coupon_required",
            "The supplied coupon is not a waitlist coupon.",
            Type: ErrorType.Validation);

        public static Error WaitlistCouponTicketMissing(TicketTypeId ticketTypeId) => new(
            "update_registration.waitlist_coupon_ticket_missing",
            "The final ticket selection must include the waitlist coupon's offered ticket type.",
            Type: ErrorType.Validation,
            Details: new Dictionary<string, object?> { ["ticketTypeId"] = ticketTypeId.Value });
    }
}
