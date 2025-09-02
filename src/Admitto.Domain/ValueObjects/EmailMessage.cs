namespace Amolenk.Admitto.Domain.ValueObjects;

public record EmailMessage(
    string Recipient,
    string Subject,
    string Body,
    string EmailType,
    Guid? ParticipantId = null);