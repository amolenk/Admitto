namespace Amolenk.Admitto.Domain.ValueObjects;

/// <summary>
/// Represents the settings for sending emails.
/// </summary>
public class EmailSettings
{
    public string SenderEmail { get; private set; }
    public string SmtpServer { get; private set; }
    public int SmtpPort { get; private set; }

    public EmailSettings(string senderEmail, string smtpServer, int smtpPort)
    {
        SenderEmail = senderEmail;
        SmtpServer = smtpServer;
        SmtpPort = smtpPort;
    }
}