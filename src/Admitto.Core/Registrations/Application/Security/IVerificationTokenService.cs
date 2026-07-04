namespace Amolenk.Admitto.Core.Registrations.Application.Security;

internal interface IVerificationTokenService
{
    string Issue(EmailAddress email, TicketedEventId eventId, TeamId teamId);

    VerificationTokenClaims? Validate(string token, TicketedEventId eventId);
}

internal sealed record VerificationTokenClaims(EmailAddress Email);
