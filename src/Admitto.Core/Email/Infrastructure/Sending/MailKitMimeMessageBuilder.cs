using Amolenk.Admitto.Core.Email.Application.Sending;
using Amolenk.Admitto.Core.Email.Application.Sending.Settings;
using Amolenk.Admitto.Core.Email.Domain.ValueObjects;
using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;
using MimeKit;

namespace Amolenk.Admitto.Core.Email.Infrastructure.Sending;

internal static class MailKitMimeMessageBuilder
{
    public static MimeMessage Build(EffectiveEmailSettings settings, EmailMessage message)
        => Build(settings.FromAddress, settings.ReplyToAddress, message);

    public static MimeMessage Build(EmailAddress fromAddress, EmailAddress? replyToAddress, EmailMessage message)
    {
        var mimeMessage = new MimeMessage();
        var fromDisplayName = replyToAddress is null
            ? fromAddress.Value
            : replyToAddress.Value.Value;

        mimeMessage.From.Add(new MailboxAddress(fromDisplayName, fromAddress.Value));
        if (replyToAddress is not null)
            mimeMessage.ReplyTo.Add(new MailboxAddress(replyToAddress.Value.Value, replyToAddress.Value.Value));

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
