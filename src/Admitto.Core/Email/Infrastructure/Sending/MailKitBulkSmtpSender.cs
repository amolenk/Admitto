using Amolenk.Admitto.Core.Email.Application.Sending;
using Amolenk.Admitto.Core.Email.Application.Sending.Bulk;
using Amolenk.Admitto.Core.Email.Application.Sending.Settings;
using Amolenk.Admitto.Core.Email.Domain.ValueObjects;
using MailKit.Net.Smtp;
using MailKit.Security;

namespace Amolenk.Admitto.Core.Email.Infrastructure.Sending;

internal sealed class MailKitBulkSmtpSender : IBulkSmtpSender
{
    public string Provider => "MailKit/SMTP";

    public async Task<IBulkSmtpSession> OpenSessionAsync(
        EffectiveEmailSettings settings,
        CancellationToken cancellationToken = default)
    {
        var client = new SmtpClient();

        var secureSocketOptions = GetSecureSocketOptions(settings);

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

        return new MailKitBulkSmtpSession(
            client,
            settings.FromAddress,
            settings.FromDisplayName,
            settings.ReplyToAddress,
            settings.ReplyToDisplayName);
    }

    private static SecureSocketOptions GetSecureSocketOptions(EffectiveEmailSettings settings) =>
        settings.SmtpSsl
            ? SecureSocketOptions.SslOnConnect
            : settings.SmtpStartTls
                ? SecureSocketOptions.StartTls
                : SecureSocketOptions.None;

    private sealed class MailKitBulkSmtpSession(
        SmtpClient client,
        EmailAddress fromAddress,
        string fromDisplayName,
        EmailAddress? replyToAddress,
        string? replyToDisplayName) : IBulkSmtpSession
    {
        public async Task<string?> SendAsync(EmailMessage message, CancellationToken cancellationToken = default)
        {
            var mimeMessage = MailKitMimeMessageBuilder.Build(
                fromAddress,
                fromDisplayName,
                replyToAddress,
                replyToDisplayName,
                message);
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
    }
}
