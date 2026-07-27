using Amolenk.Admitto.Core.Email.Application.Sending;
using Amolenk.Admitto.Core.Email.Application.Sending.Settings;
using MimeKit;

namespace Amolenk.Admitto.Core.Email.Infrastructure.Sending;

/// <summary>
/// Builds the outgoing MIME message.
/// <para>
/// All application email is sent under the platform's own identity: the deployment-configured
/// system sender address and display name, with no <c>Reply-To</c> header. Sending on behalf of
/// a team (team name as display name, team address as reply-to) hurts deliverability — it makes
/// messages look like spoofed third-party mail — so sender identity is deliberately independent
/// of team data. See ADR-013.
/// </para>
/// </summary>
internal static class MailKitMimeMessageBuilder
{
    public static MimeMessage Build(EffectiveEmailSettings settings, EmailMessage message)
        => Build(settings.FromAddress, settings.FromDisplayName, message);

    public static MimeMessage Build(
        EmailAddress fromAddress,
        string fromDisplayName,
        EmailMessage message)
    {
        var mimeMessage = new MimeMessage();

        mimeMessage.From.Add(new MailboxAddress(fromDisplayName, fromAddress.Value));
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
