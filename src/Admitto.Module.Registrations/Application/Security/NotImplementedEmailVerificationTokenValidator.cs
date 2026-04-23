using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Module.Registrations.Application.Security;

// Placeholder validator: the real email-verification token issuer + validator
// has not been implemented yet. Until it ships, self-service registrations that
// supply a token will hit this implementation and surface as a 500-class error.
// Self-service registrations that omit the token are short-circuited by the
// handler with `email.verification_required` *before* the validator is called,
// so the placeholder is only reached when a token is actually supplied.
//
// Swap this DI registration in `DependencyInjection.cs` for the real validator
// when it lands; no handler or command changes are required.
internal sealed class NotImplementedEmailVerificationTokenValidator : IEmailVerificationTokenValidator
{
    public ValueTask<EmailVerificationResult> ValidateAsync(
        string token,
        EmailAddress expectedEmail,
        CancellationToken cancellationToken)
        => throw new NotImplementedException(
            "Email-verification token validation has not been implemented yet. "
            + "Replace the IEmailVerificationTokenValidator registration with the real validator "
            + "before enabling self-service registration.");
}
