using Amolenk.Admitto.Core.Email.Domain.ValueObjects;
using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Core.Email.Application.Sending.Settings;

/// <summary>
/// Resolved and decrypted email settings ready for use by the send pipeline.
/// </summary>
public sealed record EffectiveEmailSettings(
    Hostname SmtpHost,
    Port SmtpPort,
    bool SmtpSsl,
    bool SmtpStartTls,
    EmailAddress FromAddress,
    EmailAddress? ReplyToAddress,
    EmailAuthMode AuthMode,
    string? Username,
    string? Password,
    EmailAccentColor AccentColor,
    EmailFontFamily FontFamily)
{
    public bool IsValid() =>
        AuthMode == EmailAuthMode.None
        || (AuthMode == EmailAuthMode.Basic && Username is not null && Password is not null);
}
