using Amolenk.Admitto.Module.Email.Application.Sending;
using Amolenk.Admitto.Module.Email.Application.Settings;

namespace Amolenk.Admitto.Module.Email.Tests.Application.UseCases.SendEmail;

/// <summary>
/// Fake email sender for integration tests. Captures sent messages.
/// </summary>
internal sealed class FakeEmailSender : IEmailSender
{
    public string Provider => "Fake";

    public List<(EffectiveEmailSettings Settings, EmailMessage Message)> SentMessages { get; } = [];

    public bool ShouldThrow { get; set; }

    public ValueTask<string?> SendAsync(
        EffectiveEmailSettings settings,
        EmailMessage message,
        CancellationToken cancellationToken = default)
    {
        if (ShouldThrow)
            throw new InvalidOperationException("SMTP error (fake)");

        SentMessages.Add((settings, message));
        return ValueTask.FromResult<string?>($"msg-{SentMessages.Count}");
    }
}
