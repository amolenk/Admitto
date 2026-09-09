using Amolenk.Admitto.Core.Email.Application.Sending;
using Amolenk.Admitto.Core.Email.Application.Sending.Settings;
using Amolenk.Admitto.Core.Email.Domain.ValueObjects;
using MailKit.Net.Smtp;
using MailKit.Security;

namespace Amolenk.Admitto.Core.Email.Infrastructure.Sending;

internal sealed class MailKitSmtpBatchSender : ISmtpBatchSender
{
    public string Provider => "MailKit/SMTP";

    public async Task<ISmtpBatchSession> OpenSessionAsync(
        SmtpTransportSettings settings,
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

        return new MailKitSmtpBatchSession(
            client,
            settings.FromAddress,
            settings.FromDisplayName);
    }

    private static SecureSocketOptions GetSecureSocketOptions(SmtpTransportSettings settings) =>
        settings.SmtpSsl
            ? SecureSocketOptions.SslOnConnect
            : settings.SmtpStartTls
                ? SecureSocketOptions.StartTls
                : SecureSocketOptions.None;

    private sealed class MailKitSmtpBatchSession(
        SmtpClient client,
        EmailAddress fromAddress,
        string fromDisplayName) : ISmtpBatchSession
    {
        public async Task<string?> SendAsync(EmailMessage message, CancellationToken cancellationToken = default)
        {
            var mimeMessage = MailKitMimeMessageBuilder.Build(fromAddress, fromDisplayName, message);
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
