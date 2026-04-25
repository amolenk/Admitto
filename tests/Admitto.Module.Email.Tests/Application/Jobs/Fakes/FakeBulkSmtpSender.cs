using Amolenk.Admitto.Module.Email.Application.Sending;
using Amolenk.Admitto.Module.Email.Application.Sending.Bulk;
using Amolenk.Admitto.Module.Email.Application.Sending.Settings;

namespace Amolenk.Admitto.Module.Email.Tests.Application.Jobs.Fakes;

/// <summary>
/// Fake bulk SMTP sender used by <see cref="Application.Jobs.SendBulkEmailJob"/>
/// integration tests. Records the number of sessions opened and every message
/// that flows through them; supports per-recipient failure injection plus a
/// hook for test-side actions to run as a side effect of <c>SendAsync</c>
/// (e.g. requesting cancellation).
/// </summary>
internal sealed class FakeBulkSmtpSender : IBulkSmtpSender
{
    private readonly HashSet<string> _failOn = new(StringComparer.OrdinalIgnoreCase);
    public string Provider => "FakeBulk";

    public int SessionsOpened { get; private set; }
    public int SessionsClosed { get; private set; }
    public List<EmailMessage> SentMessages { get; } = [];
    public Func<EmailMessage, Task>? OnBeforeSendAsync { get; set; }

    public void FailOn(string recipientEmail) => _failOn.Add(recipientEmail);

    public Task<IBulkSmtpSession> OpenSessionAsync(
        EffectiveEmailSettings settings,
        CancellationToken cancellationToken = default)
    {
        SessionsOpened++;
        return Task.FromResult<IBulkSmtpSession>(new Session(this));
    }

    private sealed class Session(FakeBulkSmtpSender owner) : IBulkSmtpSession
    {
        public async Task<string?> SendAsync(EmailMessage message, CancellationToken cancellationToken = default)
        {
            if (owner.OnBeforeSendAsync is not null)
                await owner.OnBeforeSendAsync(message);

            cancellationToken.ThrowIfCancellationRequested();

            if (owner._failOn.Contains(message.RecipientAddress))
                throw new InvalidOperationException($"SMTP error (fake) for {message.RecipientAddress}");

            owner.SentMessages.Add(message);
            return $"msg-{owner.SentMessages.Count}";
        }

        public ValueTask DisposeAsync()
        {
            owner.SessionsClosed++;
            return ValueTask.CompletedTask;
        }
    }
}
