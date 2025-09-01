using Amolenk.Admitto.Domain.ValueObjects;

namespace Amolenk.Admitto.Application.Common.Email.Composing;

/// <summary>
/// Represents the parameters required to compose an email to an unverified user.
/// </summary>
public record VerificationEmailParameters(
    string EventName,
    string EventWebsite,
    string Recipient,
    EmailRecipientType RecipientType,
    string VerificationCode)
    : IEmailParameters;