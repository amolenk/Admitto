using Amolenk.Admitto.Module.Email.Application.Settings;

namespace Amolenk.Admitto.Module.Email.Application.Sending;

/// <summary>
/// Sends a rendered email via a transport (SMTP/API).
/// </summary>
public interface IEmailSender
{
    /// <summary>
    /// A human-readable name used in email log entries (e.g. "MailKit/SMTP").
    /// </summary>
    string Provider { get; }

    ValueTask<string?> SendAsync(
        EffectiveEmailSettings settings,
        EmailMessage message,
        CancellationToken cancellationToken = default);
}

public sealed record EmailMessage(
    string RecipientAddress,
    string RecipientName,
    string Subject,
    string TextBody,
    string HtmlBody);
