using Amolenk.Admitto.Core.Email.Application.Sending;
using Amolenk.Admitto.Core.Email.Application.Sending.Settings;
using Amolenk.Admitto.Core.Email.Domain.ValueObjects;
using MailKit.Net.Smtp;
using MailKit.Security;

namespace Amolenk.Admitto.Core.Email.Infrastructure.Sending;

internal sealed class MailKitEmailSender : IEmailSender
{
    public string Provider => "MailKit/SMTP";

    public async ValueTask<string?> SendAsync(
        EffectiveEmailSettings settings,
        EmailMessage message,
        CancellationToken cancellationToken = default)
    {
        var mimeMessage = MailKitMimeMessageBuilder.Build(settings, message);

        using var client = new SmtpClient();

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

        var result = await client.SendAsync(mimeMessage, cancellationToken);
        await client.DisconnectAsync(quit: true, cancellationToken);

        return result;
    }

    private static SecureSocketOptions GetSecureSocketOptions(EffectiveEmailSettings settings) =>
        settings.SmtpSsl
            ? SecureSocketOptions.SslOnConnect
            : settings.SmtpStartTls
                ? SecureSocketOptions.StartTls
                : SecureSocketOptions.None;
}
