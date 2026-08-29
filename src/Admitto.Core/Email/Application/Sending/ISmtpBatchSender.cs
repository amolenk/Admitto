using Amolenk.Admitto.Core.Email.Application.Sending.Settings;

namespace Amolenk.Admitto.Core.Email.Application.Sending;

/// <summary>
/// Session-mode SMTP sender shared by generic bulk and reconfirmation batches.
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
