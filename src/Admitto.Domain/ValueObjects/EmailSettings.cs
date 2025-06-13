namespace Amolenk.Admitto.Domain.ValueObjects;

/// <summary>
/// Represents the settings for sending emails.
/// </summary>
public record EmailSettings(string SenderEmail, string SmtpServer, int SmtpPort);
