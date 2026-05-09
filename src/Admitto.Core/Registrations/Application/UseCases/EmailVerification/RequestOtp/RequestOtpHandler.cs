using Amolenk.Admitto.Core.Registrations.Application.Persistence;
using Amolenk.Admitto.Core.Registrations.Application.Security;
using Amolenk.Admitto.Core.Registrations.Domain.Entities;
using Amolenk.Admitto.Core.Shared.Application.Messaging;
using Amolenk.Admitto.Core.Shared.Kernel.ErrorHandling;
using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Amolenk.Admitto.Core.Registrations.Application.UseCases.EmailVerification.RequestOtp;

internal sealed class RequestOtpHandler(
    IRegistrationsWriteStore writeStore,
    TimeProvider timeProvider,
    IOptions<OtpOptions> options) : ICommandHandler<RequestOtpCommand>
{
    public async ValueTask HandleAsync(
        RequestOtpCommand command,
        CancellationToken cancellationToken)
    {
        var emailResult = EmailAddress.TryFrom(command.Email);
        if (!emailResult.IsSuccess)
            throw new BusinessRuleViolationException(Errors.InvalidEmail);

        var email = emailResult.ValueObject;

        var ticketedEvent = await writeStore.TicketedEvents
            .FirstOrDefaultAsync(e => e.Id == command.EventId, cancellationToken);

        if (ticketedEvent is null)
            throw new BusinessRuleViolationException(Errors.EventNotFound);

        if (!ticketedEvent.IsActive)
            throw new BusinessRuleViolationException(Errors.EventNotActive);

        var now = timeProvider.GetUtcNow();
        var rateLimitWindow = now.AddMinutes(-options.Value.RateLimitWindowMinutes);

        var emailHash = OtpCode.ComputeEmailHash(email.Value);

        var recentCount = await writeStore.OtpCodes
            .CountAsync(
                c => c.EventId == command.EventId
                     && c.EmailHash == emailHash
                     && c.ExpiresAt > rateLimitWindow,
                cancellationToken);

        if (recentCount >= options.Value.MaxRequestsPerWindow)
            throw new BusinessRuleViolationException(Errors.TooManyRequests);

        // Supersede any active (non-expired, non-used, non-superseded) codes for this email+event.
        var activeCodes = await writeStore.OtpCodes
            .Where(c => c.EventId == command.EventId
                        && c.EmailHash == emailHash
                        && c.SupersededAt == null
                        && c.UsedAt == null
                        && c.ExpiresAt > now)
            .ToListAsync(cancellationToken);

        foreach (var active in activeCodes)
        {
            active.Supersede(now);
        }

        var plainCode = GenerateSixDigitCode();
        var expiresAt = now.AddMinutes(options.Value.ExpiryMinutes);

        var otpCode = OtpCode.Create(command.TeamId, command.EventId, ticketedEvent.Name.Value, email, plainCode, expiresAt);
        await writeStore.OtpCodes.AddAsync(otpCode, cancellationToken);
    }

    private static string GenerateSixDigitCode()
    {
        var value = Random.Shared.Next(0, 1_000_000);
        return value.ToString("D6");
    }

    internal static class Errors
    {
        public static readonly Error EventNotFound = new(
            "otp.event_not_found",
            "The ticketed event could not be found.",
            Type: ErrorType.NotFound);

        public static readonly Error EventNotActive = new(
            "otp.event_not_active",
            "The event is not accepting registrations.",
            Type: ErrorType.Validation);

        public static readonly Error InvalidEmail = new(
            "otp.invalid_email",
            "The provided email address is not valid.",
            Type: ErrorType.Validation);

        public static readonly Error TooManyRequests = new(
            "otp.too_many_requests",
            "Too many OTP requests. Please wait before requesting another code.",
            Type: ErrorType.Validation);
    }
}
