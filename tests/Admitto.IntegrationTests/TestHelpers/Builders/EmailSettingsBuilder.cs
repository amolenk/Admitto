using Amolenk.Admitto.Domain.ValueObjects;

namespace Amolenk.Admitto.IntegrationTests.TestHelpers.Builders;

public class EmailSettingsBuilder
{
    private string _senderEmail = "no-reply@example.com";
    private string _smtpServer = "smtp.example.com";
    private int _port = 587;
    private string _username = "user@example.com";
    private string _password = "password";

    public EmailSettingsBuilder WithSenderEmail(string senderEmail)
    {
        _senderEmail = senderEmail;
        return this;
    }

    public EmailSettingsBuilder WithSmtpServer(string smtpServer)
    {
        _smtpServer = smtpServer;
        return this;
    }

    public EmailSettingsBuilder WithPort(int port)
    {
        _port = port;
        return this;
    }

    public EmailSettingsBuilder WithUsername(string username)
    {
        _username = username;
        return this;
    }

    public EmailSettingsBuilder WithPassword(string password)
    {
        _password = password;
        return this;
    }

    public EmailSettings Build()
    {
        return new EmailSettings(_senderEmail, _smtpServer, _port);
    }
}