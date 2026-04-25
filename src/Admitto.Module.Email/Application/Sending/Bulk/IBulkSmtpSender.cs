using Amolenk.Admitto.Module.Email.Application.Sending.Settings;

namespace Amolenk.Admitto.Module.Email.Application.Sending.Bulk;

/// <summary>
/// Session-mode SMTP sender used by bulk fan-out so a single SMTP connection
/// (per worker pickup) serves many messages, per design D4. Distinct from
/// <see cref="IEmailSender"/> which opens a new connection per single-send.
/// </summary>
public interface IBulkSmtpSender
{
    /// <summary>Human-readable provider name written into <c>email_log.provider</c>.</summary>
    string Provider { get; }

    Task<IBulkSmtpSession> OpenSessionAsync(
        EffectiveEmailSettings settings,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// An open SMTP session. <see cref="DisposeAsync"/> SHALL close the underlying
/// connection cleanly (QUIT). Returned by
/// <see cref="IBulkSmtpSender.OpenSessionAsync"/>.
/// </summary>
public interface IBulkSmtpSession : IAsyncDisposable
{
    Task<string?> SendAsync(EmailMessage message, CancellationToken cancellationToken = default);
}
