using Amolenk.Admitto.Core.Email.Application.Sending;
using Amolenk.Admitto.Core.Email.Application.Sending.Settings;

namespace Amolenk.Admitto.Core.IntegrationTests.Email.Application.UseCases.Emails.SendEmail;

/// <summary>
/// Fake email sender for integration tests. Captures sent messages.
/// </summary>
internal sealed class FakeEmailSender : IEmailSender
{
    public string Provider => "Fake";

    public List<(EffectiveEmailSettings Settings, EmailMessage Message)> SentMessages { get; } = [];
    public int SendAttempts { get; private set; }

    public bool ShouldThrow { get; set; }

    public ValueTask<string?> SendAsync(
        EffectiveEmailSettings settings,
        EmailMessage message,
        CancellationToken cancellationToken = default)
    {
        SendAttempts++;

        if (ShouldThrow)
            throw new InvalidOperationException("SMTP error (fake)");

        SentMessages.Add((settings, message));
        return ValueTask.FromResult<string?>($"msg-{SentMessages.Count}");
    }
}
