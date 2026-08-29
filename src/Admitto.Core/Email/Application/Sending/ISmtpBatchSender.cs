using Amolenk.Admitto.Core.Email.Application.Sending.Settings;

namespace Amolenk.Admitto.Core.Email.Application.Sending;

/// <summary>
/// Session-mode SMTP sender used by reconfirmation batches.
/// </summary>
public interface ISmtpBatchSender
{
    string Provider { get; }

    Task<ISmtpBatchSession> OpenSessionAsync(
        EffectiveEmailSettings settings,
        CancellationToken cancellationToken = default);
}

public interface ISmtpBatchSession : IAsyncDisposable
{
    Task<string?> SendAsync(
        EmailMessage message,
        CancellationToken cancellationToken = default);
}
