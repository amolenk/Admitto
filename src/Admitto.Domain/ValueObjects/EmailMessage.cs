namespace Amolenk.Admitto.Domain.ValueObjects;

public record EmailMessage(
    string Recipient,
    EmailRecipientType RecipientType,
    string Subject,
    string Body,
    EmailType EmailType);