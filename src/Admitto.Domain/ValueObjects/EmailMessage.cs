namespace Amolenk.Admitto.Domain.ValueObjects;

public record EmailMessage(string Recipient, string Subject, string Body, EmailType EmailType);