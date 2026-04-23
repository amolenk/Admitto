using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Module.Registrations.Application.Security;

internal interface IEmailVerificationTokenValidator
{
    ValueTask<EmailVerificationResult> ValidateAsync(
        string token,
        EmailAddress expectedEmail,
        CancellationToken cancellationToken);
}

internal readonly record struct EmailVerificationResult(bool IsValid, string? FailureReason = null);
