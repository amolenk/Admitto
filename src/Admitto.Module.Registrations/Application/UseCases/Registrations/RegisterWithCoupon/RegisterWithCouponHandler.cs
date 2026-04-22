using Amolenk.Admitto.Module.Registrations.Application.Persistence;
using Amolenk.Admitto.Module.Registrations.Application.UseCases.Registrations.SelfRegisterAttendee;
using Amolenk.Admitto.Module.Registrations.Domain.Entities;
using Amolenk.Admitto.Module.Registrations.Domain.ValueObjects;
using Amolenk.Admitto.Module.Shared.Application.Messaging;
using Amolenk.Admitto.Module.Shared.Kernel.ErrorHandling;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Module.Registrations.Application.UseCases.Registrations.RegisterWithCoupon;

internal sealed class RegisterWithCouponHandler(
    IRegistrationsWriteStore writeStore,
    TimeProvider timeProvider)
    : ICommandHandler<RegisterWithCouponCommand, RegistrationId>
{
    public async ValueTask<RegistrationId> HandleAsync(
        RegisterWithCouponCommand command,
        CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(command.CouponCode, out var codeGuid))
            throw new BusinessRuleViolationException(Errors.CouponNotFound);

        var coupon = await writeStore.Coupons
            .FirstOrDefaultAsync(
                c => c.EventId == command.EventId && c.Code == new CouponCode(codeGuid),
                cancellationToken);

        if (coupon is null)
            throw new BusinessRuleViolationException(Errors.CouponNotFound);

        var now = timeProvider.GetUtcNow();
        var status = coupon.GetStatus(now);

        switch (status)
        {
            case CouponStatus.Expired:
                throw new BusinessRuleViolationException(Errors.CouponExpired);
            case CouponStatus.Redeemed:
                throw new BusinessRuleViolationException(Errors.CouponAlreadyRedeemed);
            case CouponStatus.Revoked:
                throw new BusinessRuleViolationException(Errors.CouponRevoked);
        }

        var notAllowlisted = command.TicketTypeSlugs
            .Where(s => !coupon.AllowedTicketTypeSlugs.Contains(s))
            .ToArray();
        if (notAllowlisted.Length > 0)
            throw new BusinessRuleViolationException(Errors.TicketTypeNotAllowlisted(notAllowlisted));

        var ticketedEvent = await writeStore.TicketedEvents
            .FirstOrDefaultAsync(e => e.Id == command.EventId, cancellationToken);

        if (ticketedEvent is null)
            throw new BusinessRuleViolationException(Errors.EventNotFound);

        // Coupons bypass capacity/window/domain per the coupon rules, but SHALL NOT bypass
        // the active-status gate.
        if (!ticketedEvent.IsActive)
            throw new BusinessRuleViolationException(Errors.EventNotActive);

        if (!coupon.BypassRegistrationWindow)
        {
            var policy = ticketedEvent.RegistrationPolicy;
            if (policy is null)
                throw new BusinessRuleViolationException(Errors.RegistrationNotOpen);

            if (now < policy.OpensAt)
                throw new BusinessRuleViolationException(Errors.RegistrationNotOpen);

            if (now >= policy.ClosesAt)
                throw new BusinessRuleViolationException(Errors.RegistrationClosed);
        }

        var catalog = await writeStore.TicketCatalogs
            .FirstOrDefaultAsync(tc => tc.Id == command.EventId, cancellationToken);

        if (catalog is not null)
        {
            var ticketTypeMap = catalog.TicketTypes.ToDictionary(t => t.Id);
            SelfRegisterAttendeeHandler.ValidateTicketTypeSelection(
                command.TicketTypeSlugs, ticketTypeMap);
        }

        var tickets = command.TicketTypeSlugs
            .Select(slug =>
            {
                var tt = catalog?.GetTicketType(slug);
                var timeSlots = tt?.TimeSlots.Select(ts => ts.Slug.Value).ToArray() ?? [];
                return new TicketTypeSnapshot(slug, timeSlots);
            })
            .ToList();

        if (catalog is not null)
        {
            try
            {
                catalog.Claim(command.TicketTypeSlugs, enforce: false);
            }
            catch (BusinessRuleViolationException ex)
                when (ex.Error.Code == TicketCatalog.Errors.EventNotActive.Code)
            {
                throw new BusinessRuleViolationException(Errors.EventNotActive);
            }
        }

        coupon.Redeem();

        var registration = Registration.Create(command.EventId, command.Email, tickets);
        await writeStore.Registrations.AddAsync(registration, cancellationToken);

        return registration.Id;
    }

    internal static class Errors
    {
        public static readonly Error CouponNotFound = new(
            "coupon.not_found",
            "Coupon not found.",
            Type: ErrorType.NotFound);

        public static readonly Error CouponExpired = new(
            "coupon.expired",
            "This coupon has expired.",
            Type: ErrorType.Validation);

        public static readonly Error CouponAlreadyRedeemed = new(
            "coupon.already_redeemed",
            "This coupon has already been used.",
            Type: ErrorType.Conflict);

        public static readonly Error CouponRevoked = new(
            "coupon.revoked",
            "This coupon has been revoked.",
            Type: ErrorType.Conflict);

        public static Error TicketTypeNotAllowlisted(string[] slugs) => new(
            "coupon.ticket_type_not_allowed",
            "One or more ticket types are not allowed for this coupon.",
            Details: new Dictionary<string, object?> { ["slugs"] = slugs });

        public static readonly Error EventNotFound = new(
            "registration.event_not_found",
            "The ticketed event could not be found.",
            Type: ErrorType.NotFound);

        public static readonly Error EventNotActive = new(
            "registration.event_not_active",
            "Cannot register for a cancelled or archived event.",
            Type: ErrorType.Validation);

        public static readonly Error RegistrationNotOpen = new(
            "registration.not_open",
            "Registration is not open for this event.",
            Type: ErrorType.Validation);

        public static readonly Error RegistrationClosed = new(
            "registration.closed",
            "Registration for this event has closed.",
            Type: ErrorType.Validation);
    }
}
