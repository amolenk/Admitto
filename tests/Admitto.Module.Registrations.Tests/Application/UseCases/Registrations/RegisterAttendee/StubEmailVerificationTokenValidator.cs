using Amolenk.Admitto.Module.Registrations.Application.Security;
using Amolenk.Admitto.Module.Registrations.Domain.ValueObjects;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Module.Registrations.Tests.Application.UseCases.Registrations.RegisterAttendee;

/// <summary>
/// Test stub for <see cref="IVerificationTokenService"/>.
/// A token is considered valid when it equals <c>"VALID-TOKEN-FOR-{email}"</c>.
/// </summary>
internal sealed class StubEmailVerificationTokenValidator : IVerificationTokenService
{
    public static string ValidTokenFor(string email) => $"VALID-TOKEN-FOR-{email}";

    public string Issue(EmailAddress email, TicketedEventId eventId, TeamId teamId)
        => ValidTokenFor(email.Value);

    public VerificationTokenClaims? Validate(string token, TicketedEventId eventId)
    {
        // Tokens follow the pattern "VALID-TOKEN-FOR-{email}"
        const string prefix = "VALID-TOKEN-FOR-";
        if (!token.StartsWith(prefix, StringComparison.Ordinal))
            return null;

        var emailValue = token[prefix.Length..];
        var emailResult = EmailAddress.TryFrom(emailValue);
        return emailResult.IsSuccess ? new VerificationTokenClaims(emailResult.Value) : null;
    }
}
