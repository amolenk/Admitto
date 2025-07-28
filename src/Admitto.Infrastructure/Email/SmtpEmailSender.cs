using Amolenk.Admitto.Application.Common.Email;
using Amolenk.Admitto.Application.Common.Email.Sending;
using Amolenk.Admitto.Domain.ValueObjects;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace Amolenk.Admitto.Infrastructure.Email;

public record SmtpSettings
{
    public string Host { get; init; } = null!;
    public int Port { get; init; }
    public string? Username { get; init; }
    public string? Password { get; init; }
    public string FromName { get; init; } = null!;
    public string FromEmail { get; init; } = null!;
    public SecureSocketOptions SecureSocketOptions { get; init; }
}

public class SmtpEmailSender : IEmailSender
{
    private readonly MailboxAddress _from;
    private readonly SmtpClient _client;
    
    private SmtpEmailSender(MailboxAddress from, SmtpClient client)
    {
        _from = from;
        _client = client;
    }

    public static async ValueTask<SmtpEmailSender> ConnectAsync(SmtpSettings settings)
    {
        var client = new SmtpClient();
        await client.ConnectAsync(settings.Host, settings.Port, settings.SecureSocketOptions);

        if (!string.IsNullOrWhiteSpace(settings.Username))
        {
            await client.AuthenticateAsync(settings.Username, settings.Password);
        }

        var from = new MailboxAddress(settings.FromName, settings.FromEmail);
        
        return new SmtpEmailSender(from, client);
    }
    
    public async ValueTask SendEmailAsync(EmailMessage emailMessage)
    {
        var message = new MimeMessage
        {
            Subject = emailMessage.Subject,
            Body = new TextPart("html") { Text = emailMessage.Body }
        };

        message.From.Add(_from);
        message.To.Add(MailboxAddress.Parse(emailMessage.Recipient));
        
        await _client.SendAsync(message);
    }
    
    public void Dispose()
    {
        _client.Disconnect(true);
        _client.Dispose();
    }
}