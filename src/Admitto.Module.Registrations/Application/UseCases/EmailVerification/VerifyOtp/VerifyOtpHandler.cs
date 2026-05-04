using System.Security.Cryptography;
using System.Text;
using Amolenk.Admitto.Module.Registrations.Application.Persistence;
using Amolenk.Admitto.Module.Registrations.Application.Security;
using Amolenk.Admitto.Module.Shared.Application.Messaging;
using Amolenk.Admitto.Module.Shared.Kernel.ErrorHandling;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace Amolenk.Admitto.Module.Registrations.Application.UseCases.EmailVerification.VerifyOtp;

internal sealed class VerifyOtpHandler(
    IRegistrationsWriteStore writeStore,
    IVerificationTokenService verificationTokenService,
    TimeProvider timeProvider) : ICommandHandler<VerifyOtpCommand, string>
{
    public async ValueTask<string> HandleAsync(
        VerifyOtpCommand command,
        CancellationToken cancellationToken)
    {
        var emailResult = EmailAddress.TryFrom(command.Email);
        if (!emailResult.IsSuccess)
            throw new BusinessRuleViolationException(Errors.InvalidCode);

        var email = emailResult.Value;
        var emailHash = ComputeHash(email.Value.ToLowerInvariant());
        var now = timeProvider.GetUtcNow();

        // Load the latest non-superseded OTP code for this email+event.
        var otpCode = await writeStore.OtpCodes
            .Where(c => c.EventId == command.EventId
                        && c.EmailHash == emailHash
                        && c.SupersededAt == null)
            .OrderByDescending(c => c.ExpiresAt)
            .FirstOrDefaultAsync(cancellationToken);

        if (otpCode is null || otpCode.IsExpired(now) || otpCode.IsUsed)
            throw new BusinessRuleViolationException(Errors.InvalidCode);

        if (otpCode.IsLocked)
            throw new BusinessRuleViolationException(Errors.CodeLocked);

        var codeHash = ComputeHash(command.Code);
        if (!CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(codeHash),
                Encoding.UTF8.GetBytes(otpCode.CodeHash)))
        {
            otpCode.IncrementFailedAttempts();
            throw new BusinessRuleViolationException(otpCode.IsLocked
                ? Errors.CodeLocked
                : Errors.InvalidCode);
        }

        otpCode.MarkUsed(now);

        return verificationTokenService.Issue(email, command.EventId, command.TeamId);
    }

    private static string ComputeHash(string value)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(value));
        return Convert.ToHexStringLower(bytes);
    }

    internal static class Errors
    {
        public static readonly Error InvalidCode = new(
            "otp.invalid_code",
            "The OTP code is invalid or expired.",
            Type: ErrorType.Validation);

        public static readonly Error CodeLocked = new(
            "otp.code_locked",
            "Too many failed attempts. Please request a new OTP code.",
            Type: ErrorType.Validation);
    }
}
