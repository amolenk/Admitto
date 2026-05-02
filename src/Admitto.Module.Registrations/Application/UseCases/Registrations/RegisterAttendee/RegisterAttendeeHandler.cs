using Amolenk.Admitto.Module.Registrations.Application.Persistence;
using Amolenk.Admitto.Module.Registrations.Application.Security;
using Amolenk.Admitto.Module.Registrations.Domain.Entities;
using Amolenk.Admitto.Module.Registrations.Domain.ValueObjects;
using Amolenk.Admitto.Module.Shared.Application.Messaging;
using Amolenk.Admitto.Module.Shared.Kernel.ErrorHandling;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Module.Registrations.Application.UseCases.Registrations.RegisterAttendee;

internal sealed class RegisterAttendeeHandler(
    IRegistrationsWriteStore writeStore,
    TimeProvider timeProvider,
    IEmailVerificationTokenValidator emailVerificationTokenValidator)
    : ICommandHandler<RegisterAttendeeCommand, RegistrationId>
{
    public async ValueTask<RegistrationId> HandleAsync(
        RegisterAttendeeCommand command,
        CancellationToken cancellationToken)
    {
        // Command invariants per design D1/D8.
        if (command.Mode == RegistrationMode.Coupon && command.CouponCode is null)
            throw new InvalidOperationException(
                "Coupon mode requires a CouponCode on the command.");
        if (command.Mode != RegistrationMode.Coupon && command.CouponCode is not null)
            throw new InvalidOperationException(
                "CouponCode must only be supplied for Coupon mode.");

        // 1. Self-service email-verification check runs FIRST so that token-related
        //    failures do not leak information about other resources (per spec).
        if (command.Mode == RegistrationMode.SelfService)
        {
            if (command.EmailVerificationToken is null)
                throw new BusinessRuleViolationException(Errors.EmailVerificationRequired);

            var verification = await emailVerificationTokenValidator.ValidateAsync(
                command.EmailVerificationToken, command.Email, cancellationToken);

            if (!verification.IsValid)
                throw new BusinessRuleViolationException(Errors.EmailVerificationInvalid);
        }

        // 2. Coupon load + status + allowlist + target-email match.
        Coupon? coupon = null;
        if (command.Mode == RegistrationMode.Coupon)
        {
            if (!Guid.TryParse(command.CouponCode, out var codeGuid))
                throw new BusinessRuleViolationException(Errors.CouponNotFound);

            coupon = await writeStore.Coupons
                .FirstOrDefaultAsync(
                    c => c.EventId == command.EventId && c.Code == new CouponCode(codeGuid),
                    cancellationToken);

            if (coupon is null)
                throw new BusinessRuleViolationException(Errors.CouponNotFound);

            var status = coupon.GetStatus(timeProvider.GetUtcNow());
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

            // Bind the bearer credential to the email it was issued for (design D8).
            if (coupon.Email != command.Email)
                throw new BusinessRuleViolationException(Errors.CouponEmailMismatch);
        }

        // 3. Load TicketedEvent.
        var ticketedEvent = await writeStore.TicketedEvents
            .FirstOrDefaultAsync(e => e.Id == command.EventId, cancellationToken);

        if (ticketedEvent is null)
            throw new BusinessRuleViolationException(Errors.EventNotFound);

        // 4. Active-status gate.
        if (!ticketedEvent.IsActive)
            throw new BusinessRuleViolationException(Errors.EventNotActive);

        // 5. Window/domain checks (mode-gated).
        var now = timeProvider.GetUtcNow();
        if (command.Mode == RegistrationMode.SelfService)
        {
            EnforceRegistrationWindow(ticketedEvent.RegistrationPolicy, now);
            EnforceEmailDomain(ticketedEvent.RegistrationPolicy, command.Email);
        }
        else if (command.Mode == RegistrationMode.Coupon && !coupon!.BypassRegistrationWindow)
        {
            EnforceRegistrationWindow(ticketedEvent.RegistrationPolicy, now);
        }

        // 6. Load TicketCatalog.
        var catalog = await writeStore.TicketCatalogs
            .FirstOrDefaultAsync(tc => tc.Id == command.EventId, cancellationToken);

        if (catalog is null && command.Mode != RegistrationMode.Coupon)
            throw new BusinessRuleViolationException(Errors.NoTicketTypesConfigured);

        // 7. Validate ticket-type selection (skipped only when coupon mode has no catalog).
        Dictionary<string, TicketType>? ticketTypeMap = null;
        if (catalog is not null)
        {
            ticketTypeMap = catalog.TicketTypes.ToDictionary(t => t.Id);
            ValidateTicketTypeSelection(command.TicketTypeSlugs, ticketTypeMap);
        }

        // 8. Build snapshots (coupon-without-catalog yields empty time-slot arrays per legacy).
        var tickets = command.TicketTypeSlugs
            .Select(slug =>
            {
                var timeSlots = ticketTypeMap is not null
                    ? ticketTypeMap[slug].TimeSlots.Select(ts => ts.Slug.Value).ToArray()
                    : Array.Empty<string>();
                var name = ticketTypeMap is not null
                    ? ticketTypeMap[slug].Name.Value
                    : slug;
                return new TicketTypeSnapshot(slug, name, timeSlots);
            })
            .ToList();

        // 9. Atomic claim. Capacity is enforced only in self-service.
        if (catalog is not null)
        {
            try
            {
                catalog.Claim(command.TicketTypeSlugs, enforce: command.Mode == RegistrationMode.SelfService);
            }
            catch (BusinessRuleViolationException ex)
                when (ex.Error.Code == TicketCatalog.Errors.EventNotActive.Code)
            {
                throw new BusinessRuleViolationException(Errors.EventNotActive);
            }
        }

        // 10. Coupon redemption (after all gates pass).
        coupon?.Redeem();

        // 11. Validate and apply additional details.
        var additionalDetails = AdditionalDetails.Validate(
            command.AdditionalDetails,
            ticketedEvent.AdditionalDetailSchema);

        // 12. Create registration and persist.
        var registration = Registration.Create(
            ticketedEvent.TeamId,
            command.EventId,
            command.Email,
            command.FirstName,
            command.LastName,
            tickets,
            additionalDetails);
        await writeStore.Registrations.AddAsync(registration, cancellationToken);

        return registration.Id;
    }

    private static void EnforceRegistrationWindow(
        TicketedEventRegistrationPolicy? policy,
        DateTimeOffset now)
    {
        if (policy is null)
            throw new BusinessRuleViolationException(Errors.RegistrationNotOpen);

        if (now < policy.OpensAt)
            throw new BusinessRuleViolationException(Errors.RegistrationNotOpen);

        if (now >= policy.ClosesAt)
            throw new BusinessRuleViolationException(Errors.RegistrationClosed);
    }

    private static void EnforceEmailDomain(
        TicketedEventRegistrationPolicy? policy,
        EmailAddress email)
    {
        if (policy is null)
            return;

        if (!policy.IsEmailDomainAllowed(email.Value))
            throw new BusinessRuleViolationException(Errors.EmailDomainNotAllowed);
    }

    private static void ValidateTicketTypeSelection(
        string[] slugs,
        Dictionary<string, TicketType> ticketTypeMap)
    {
        var duplicates = slugs.GroupBy(s => s).Where(g => g.Count() > 1).Select(g => g.Key).ToArray();
        if (duplicates.Length > 0)
            throw new BusinessRuleViolationException(Errors.DuplicateTicketTypes(duplicates));

        var unknownSlugs = slugs.Where(s => !ticketTypeMap.ContainsKey(s)).ToArray();
        if (unknownSlugs.Length > 0)
            throw new BusinessRuleViolationException(Errors.UnknownTicketTypes(unknownSlugs));

        var cancelledSlugs = slugs.Where(s => ticketTypeMap[s].IsCancelled).ToArray();
        if (cancelledSlugs.Length > 0)
            throw new BusinessRuleViolationException(Errors.CancelledTicketTypes(cancelledSlugs));

        var allTimeSlots = slugs
            .SelectMany(s => ticketTypeMap[s].TimeSlots.Select(ts => ts.Slug.Value))
            .ToList();
        var overlapping = allTimeSlots.GroupBy(ts => ts).Where(g => g.Count() > 1).Select(g => g.Key).ToArray();
        if (overlapping.Length > 0)
            throw new BusinessRuleViolationException(Errors.OverlappingTimeSlots(overlapping));
    }

    internal static class Errors
    {
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

        public static readonly Error EmailDomainNotAllowed = new(
            "registration.email_domain_not_allowed",
            "Your email domain is not allowed for this event.",
            Type: ErrorType.Validation);

        public static readonly Error NoTicketTypesConfigured = new(
            "registration.no_ticket_types",
            "No ticket types have been configured for this event.",
            Type: ErrorType.Validation);

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

        public static readonly Error CouponEmailMismatch = new(
            "coupon.email_mismatch",
            "The supplied email does not match the email this coupon was issued to.",
            Type: ErrorType.Validation);

        public static readonly Error EmailVerificationRequired = new(
            "email.verification_required",
            "An email-verification token is required for self-service registration.",
            Type: ErrorType.Validation);

        public static readonly Error EmailVerificationInvalid = new(
            "email.verification_invalid",
            "The email-verification token is invalid, expired, or does not match the supplied email.",
            Type: ErrorType.Validation);
    }
}
