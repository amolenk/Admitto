using Amolenk.Admitto.Core.Email.Application.Sending;
using Amolenk.Admitto.Core.Email.Application.Sending.Settings;
using Amolenk.Admitto.Core.Email.Domain.ValueObjects;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace Amolenk.Admitto.Core.Email.Infrastructure.Sending;

internal sealed class MailKitEmailSender : IEmailSender
{
    public string Provider => "MailKit/SMTP";

    public async ValueTask<string?> SendAsync(
        EffectiveEmailSettings settings,
        EmailMessage message,
        CancellationToken cancellationToken = default)
    {
        var mimeMessage = BuildMimeMessage(settings, message);

        using var client = new SmtpClient();

        var secureSocketOptions = settings.SmtpPort.Value == 465
            ? SecureSocketOptions.SslOnConnect
            : SecureSocketOptions.StartTlsWhenAvailable;

        await client.ConnectAsync(
            settings.SmtpHost.Value,
            settings.SmtpPort.Value,
            secureSocketOptions,
            cancellationToken);

        if (settings.AuthMode == EmailAuthMode.Basic &&
            settings.Username is not null &&
            settings.Password is not null)
        {
            await client.AuthenticateAsync(settings.Username, settings.Password, cancellationToken);
        }

        var result = await client.SendAsync(mimeMessage, cancellationToken);
        await client.DisconnectAsync(quit: true, cancellationToken);

        return result;
    }

    private static MimeMessage BuildMimeMessage(EffectiveEmailSettings settings, EmailMessage message)
    {
        var mimeMessage = new MimeMessage();
        mimeMessage.From.Add(new MailboxAddress(settings.FromAddress.Value, settings.FromAddress.Value));
        if (settings.ReplyToAddress is not null)
            mimeMessage.ReplyTo.Add(new MailboxAddress(settings.ReplyToAddress.Value.Value, settings.ReplyToAddress.Value.Value));
        mimeMessage.To.Add(new MailboxAddress(message.RecipientName, message.RecipientAddress));
        mimeMessage.Subject = message.Subject;

        var body = new BodyBuilder
        {
            TextBody = message.TextBody,
            HtmlBody = message.HtmlBody
        };
        mimeMessage.Body = body.ToMessageBody();

        return mimeMessage;
    }
}
