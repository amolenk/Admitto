namespace Amolenk.Admitto.Domain.ValueObjects;

public record EmailMessage(
    string Recipient,
    string Subject,
    string TextBody,
    string HtmlBody,
    string EmailType,
    Guid? ParticipantId = null);