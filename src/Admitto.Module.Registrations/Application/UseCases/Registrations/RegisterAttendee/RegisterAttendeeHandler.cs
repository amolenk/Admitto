using Amolenk.Admitto.Module.Registrations.Contracts;
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
    IVerificationTokenService verificationTokenService)
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

            var claims = verificationTokenService.Validate(command.EmailVerificationToken, command.EventId);

            if (claims is null || claims.Email != command.Email)
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

        // 6. Explicit duplicate resolution. Active rows are rejected before any capacity
        //    claim; cancelled rows are reset after all remaining gates pass.
        var existingRegistration = await writeStore.Registrations
            .SingleOrDefaultAsync(
                r => r.EventId == command.EventId && r.Email == command.Email,
                cancellationToken);

        if (existingRegistration?.Status == RegistrationStatus.Registered)
            throw new BusinessRuleViolationException(AlreadyExistsError.Create<Registration>());

        // 7. Load TicketCatalog.
        var catalog = await writeStore.TicketCatalogs
            .FirstOrDefaultAsync(tc => tc.Id == command.EventId, cancellationToken);

        if (catalog is null && command.Mode != RegistrationMode.Coupon)
            throw new BusinessRuleViolationException(Errors.NoTicketTypesConfigured);

        // 8. Atomic claim. Validation (duplicates, unknown, cancelled, overlapping) is now
        //    enforced inside Claim. Capacity enforcement applies only in self-service mode.
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

        // 9. Build snapshots (coupon-without-catalog yields empty time-slot arrays per legacy).
        var tickets = command.TicketTypeSlugs
            .Select(slug =>
            {
                var ticketType = catalog?.GetTicketType(slug);
                var timeSlots = ticketType?.TimeSlots.Select(ts => ts.Slug.Value).ToArray()
                    ?? Array.Empty<string>();
                var name = ticketType?.Name.Value ?? slug;
                return new TicketTypeSnapshot(slug, name, timeSlots);
            })
            .ToList();

        // 10. Validate and apply additional details.
        var additionalDetails = AdditionalDetails.Validate(
            command.AdditionalDetails,
            ticketedEvent.AdditionalDetailSchema);

        // 11. Create or reset registration, then redeem the coupon after all gates pass.
        Registration registration;
        if (existingRegistration is null)
        {
            registration = Registration.Create(
                ticketedEvent.TeamId,
                command.EventId,
                command.Email,
                command.FirstName,
                command.LastName,
                tickets,
                additionalDetails);
            await writeStore.Registrations.AddAsync(registration, cancellationToken);
        }
        else
        {
            registration = existingRegistration;
            registration.Reset(
                command.FirstName,
                command.LastName,
                tickets,
                additionalDetails);
        }

        coupon?.Redeem();

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
