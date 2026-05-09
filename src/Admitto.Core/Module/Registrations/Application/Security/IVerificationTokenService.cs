using Amolenk.Admitto.Core.Module.Registrations.Domain.ValueObjects;
using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Core.Module.Registrations.Application.Security;

internal interface IVerificationTokenService
{
    string Issue(EmailAddress email, TicketedEventId eventId, TeamId teamId);

    VerificationTokenClaims? Validate(string token, TicketedEventId eventId);
}

internal sealed record VerificationTokenClaims(EmailAddress Email);
