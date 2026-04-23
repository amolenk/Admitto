using Amolenk.Admitto.Module.Registrations.Application.Security;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Module.Registrations.Tests.Application.UseCases.Registrations.RegisterAttendee;

/// <summary>
/// Test stub for <see cref="IEmailVerificationTokenValidator"/>.
/// A token is considered valid when it equals <c>"VALID-TOKEN-FOR-{email}"</c>.
/// </summary>
internal sealed class StubEmailVerificationTokenValidator : IEmailVerificationTokenValidator
{
    public static string ValidTokenFor(string email) => $"VALID-TOKEN-FOR-{email}";

    public ValueTask<EmailVerificationResult> ValidateAsync(
        string token,
        EmailAddress email,
        CancellationToken cancellationToken)
    {
        var isValid = token == ValidTokenFor(email.Value);
        return ValueTask.FromResult(new EmailVerificationResult(isValid));
    }
}
