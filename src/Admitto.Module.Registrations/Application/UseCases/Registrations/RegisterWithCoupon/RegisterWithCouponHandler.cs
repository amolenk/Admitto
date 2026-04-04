using Amolenk.Admitto.Module.Organization.Contracts;
using Amolenk.Admitto.Module.Registrations.Application.Persistence;
using Amolenk.Admitto.Module.Registrations.Domain.Entities;
using Amolenk.Admitto.Module.Registrations.Domain.ValueObjects;
using Amolenk.Admitto.Module.Shared.Application.Messaging;
using Amolenk.Admitto.Module.Shared.Kernel.ErrorHandling;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Module.Registrations.Application.UseCases.Registrations.RegisterWithCoupon;

internal sealed class RegisterWithCouponHandler(
    IOrganizationFacade organizationFacade,
    IRegistrationsWriteStore writeStore)
    : ICommandHandler<RegisterWithCouponCommand, RegistrationId>
{
    public async ValueTask<RegistrationId> HandleAsync(
        RegisterWithCouponCommand command,
        CancellationToken cancellationToken)
    {
        // Load and validate the coupon.
        if (!Guid.TryParse(command.CouponCode, out var codeGuid))
            throw new BusinessRuleViolationException(Errors.CouponNotFound);

        var coupon = await writeStore.Coupons
            .FirstOrDefaultAsync(
                c => c.EventId == command.EventId && c.Code == new CouponCode(codeGuid),
                cancellationToken);

        if (coupon is null)
            throw new BusinessRuleViolationException(Errors.CouponNotFound);

        var now = DateTimeOffset.UtcNow;
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

        // Validate all requested slugs are allowlisted by the coupon.
        var notAllowlisted = command.TicketTypeSlugs
            .Where(s => !coupon.AllowedTicketTypeSlugs.Contains(s))
            .ToArray();
        if (notAllowlisted.Length > 0)
            throw new BusinessRuleViolationException(Errors.TicketTypeNotAllowlisted(notAllowlisted));

        // Verify the event is still active.
        var isEventActive = await organizationFacade.IsEventActiveAsync(
            command.EventId.Value, cancellationToken);
        if (!isEventActive)
            throw new BusinessRuleViolationException(Errors.EventNotActive);

        // Conditionally enforce registration window.
        if (!coupon.BypassRegistrationWindow)
        {
            var policy = await writeStore.EventRegistrationPolicies
                .FirstOrDefaultAsync(p => p.Id == command.EventId, cancellationToken);

            if (policy is null || !policy.IsRegistrationOpen(now))
            {
                var isAfterWindow = policy?.RegistrationWindowClosesAt.HasValue == true
                                    && now > policy.RegistrationWindowClosesAt;
                throw new BusinessRuleViolationException(
                    isAfterWindow ? Errors.RegistrationClosed : Errors.RegistrationNotOpen);
            }
        }

        // Load and validate ticket types.
        var ticketTypeDtos = await organizationFacade.GetTicketTypesAsync(
            command.EventId.Value, cancellationToken);
        var ticketTypeMap = ticketTypeDtos.ToDictionary(t => t.Slug);
        ValidateTicketTypeSelection(command.TicketTypeSlugs, ticketTypeMap);

        // Build ticket snapshots.
        var tickets = command.TicketTypeSlugs
            .Select(slug => new TicketTypeSnapshot(slug, ticketTypeMap[slug].TimeSlots.ToArray()))
            .ToList();

        // Load or create EventCapacity, ensuring entries exist for each slug.
        var capacity = await writeStore.EventCapacities
            .FirstOrDefaultAsync(ec => ec.Id == command.EventId, cancellationToken);

        if (capacity is null)
        {
            capacity = EventCapacity.Create(command.EventId);
            writeStore.EventCapacities.Add(capacity);
        }

        // Ensure TicketCapacity entries exist (coupon bypasses max-capacity enforcement).
        foreach (var slug in command.TicketTypeSlugs)
        {
            if (capacity.TicketCapacities.All(tc => tc.Id != slug))
                capacity.SetTicketCapacity(slug, null);
        }

        // Claim uncapped — coupon-based registrations always count toward occupancy.
        capacity.Claim(command.TicketTypeSlugs, enforce: false);

        // Redeem the coupon (single-use).
        coupon.Redeem();

        // Create the registration.
        var registration = Registration.Create(command.EventId, command.Email, tickets);
        await writeStore.Registrations.AddAsync(registration, cancellationToken);

        return registration.Id;
    }

    private static void ValidateTicketTypeSelection(
        string[] slugs,
        Dictionary<string, TicketTypeDto> ticketTypeMap)
    {
        var duplicates = slugs.GroupBy(s => s).Where(g => g.Count() > 1).Select(g => g.Key).ToArray();
        if (duplicates.Length > 0)
            throw new BusinessRuleViolationException(SelfRegisterAttendeeErrors.DuplicateTicketTypes(duplicates));

        var unknownSlugs = slugs.Where(s => !ticketTypeMap.ContainsKey(s)).ToArray();
        if (unknownSlugs.Length > 0)
            throw new BusinessRuleViolationException(SelfRegisterAttendeeErrors.UnknownTicketTypes(unknownSlugs));

        var cancelledSlugs = slugs.Where(s => ticketTypeMap[s].IsCancelled).ToArray();
        if (cancelledSlugs.Length > 0)
            throw new BusinessRuleViolationException(SelfRegisterAttendeeErrors.CancelledTicketTypes(cancelledSlugs));

        var allTimeSlots = slugs.SelectMany(s => ticketTypeMap[s].TimeSlots).ToList();
        var overlapping = allTimeSlots.GroupBy(ts => ts).Where(g => g.Count() > 1).Select(g => g.Key).ToArray();
        if (overlapping.Length > 0)
            throw new BusinessRuleViolationException(SelfRegisterAttendeeErrors.OverlappingTimeSlots(overlapping));
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

    // Re-use ticket-selection error definitions from SelfRegisterAttendeeHandler.
    private static class SelfRegisterAttendeeErrors
    {
        public static Error DuplicateTicketTypes(string[] slugs) => new(
            "registration.duplicate_ticket_types",
            "Duplicate ticket types in selection.",
            Details: new Dictionary<string, object?> { ["slugs"] = slugs });

        public static Error UnknownTicketTypes(string[] slugs) => new(
            "registration.unknown_ticket_types",
            "One or more ticket types do not exist.",
            Details: new Dictionary<string, object?> { ["slugs"] = slugs });

        public static Error CancelledTicketTypes(string[] slugs) => new(
            "registration.cancelled_ticket_types",
            "One or more ticket types have been cancelled.",
            Details: new Dictionary<string, object?> { ["slugs"] = slugs });

        public static Error OverlappingTimeSlots(string[] slots) => new(
            "registration.overlapping_time_slots",
            "Selected ticket types have overlapping time slots.",
            Details: new Dictionary<string, object?> { ["slots"] = slots });
    }
}
