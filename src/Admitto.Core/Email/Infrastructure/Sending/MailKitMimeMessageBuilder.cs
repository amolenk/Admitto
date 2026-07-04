using Amolenk.Admitto.Core.Email.Application.Sending;
using Amolenk.Admitto.Core.Email.Application.Sending.Settings;
using MimeKit;

namespace Amolenk.Admitto.Core.Email.Infrastructure.Sending;

internal static class MailKitMimeMessageBuilder
{
    public static MimeMessage Build(EffectiveEmailSettings settings, EmailMessage message)
        => Build(
            settings.FromAddress,
            settings.FromDisplayName,
            settings.ReplyToAddress,
            settings.ReplyToDisplayName,
            message);

    public static MimeMessage Build(
        EmailAddress fromAddress,
        string fromDisplayName,
        EmailAddress? replyToAddress,
        string? replyToDisplayName,
        EmailMessage message)
    {
        var mimeMessage = new MimeMessage();

        // Override to fight against spam filters
        // TODO Clean-up
        fromDisplayName = "Admitto";
        fromAddress = EmailAddress.From("noreply@tickets.admitto.org");
        replyToAddress = null;
        replyToDisplayName = null;

        mimeMessage.From.Add(new MailboxAddress(fromDisplayName, fromAddress.Value));

        if (replyToAddress is not null && replyToDisplayName is not null)
            mimeMessage.ReplyTo.Add(new MailboxAddress(replyToDisplayName, replyToAddress.Value.Value));

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
