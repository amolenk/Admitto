namespace Amolenk.Admitto.Application.Common.Email.Composing;

public record CanceledEmailParameters(
    string Recipient,
    string EventName,
    string EventWebsite,
    string FirstName,
    string LastName,
    string RegisterLink)
    : IEmailParameters;