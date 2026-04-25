using Amolenk.Admitto.Module.Email.Application.Sending;
using Amolenk.Admitto.Module.Email.Application.Sending.Bulk;
using Amolenk.Admitto.Module.Email.Application.Sending.Settings;
using Amolenk.Admitto.Module.Email.Domain.ValueObjects;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace Amolenk.Admitto.Module.Email.Infrastructure.Sending;

internal sealed class MailKitBulkSmtpSender : IBulkSmtpSender
{
    public string Provider => "MailKit/SMTP";

    public async Task<IBulkSmtpSession> OpenSessionAsync(
        EffectiveEmailSettings settings,
        CancellationToken cancellationToken = default)
    {
        var client = new SmtpClient();

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

        return new MailKitBulkSmtpSession(client, settings.FromAddress);
    }

    private sealed class MailKitBulkSmtpSession(SmtpClient client, EmailAddress fromAddress) : IBulkSmtpSession
    {
        public async Task<string?> SendAsync(EmailMessage message, CancellationToken cancellationToken = default)
        {
            var mimeMessage = BuildMimeMessage(fromAddress, message);
            return await client.SendAsync(mimeMessage, cancellationToken);
        }

        public async ValueTask DisposeAsync()
        {
            try
            {
                if (client.IsConnected)
                    await client.DisconnectAsync(quit: true);
            }
            finally
            {
                client.Dispose();
            }
        }

        private static MimeMessage BuildMimeMessage(EmailAddress fromAddress, EmailMessage message)
        {
            var mimeMessage = new MimeMessage();
            mimeMessage.From.Add(new MailboxAddress(fromAddress.Value, fromAddress.Value));
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
}
