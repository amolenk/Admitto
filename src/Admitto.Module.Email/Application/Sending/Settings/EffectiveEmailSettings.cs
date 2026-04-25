using Amolenk.Admitto.Module.Email.Domain.ValueObjects;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Module.Email.Application.Settings;

/// <summary>
/// Resolved and decrypted email settings ready for use by the send pipeline.
/// </summary>
public sealed record EffectiveEmailSettings(
    Hostname SmtpHost,
    Port SmtpPort,
    EmailAddress FromAddress,
    EmailAuthMode AuthMode,
    string? Username,
    string? Password)
{
    public bool IsValid() =>
        AuthMode == EmailAuthMode.None
        || (AuthMode == EmailAuthMode.Basic && Username is not null && Password is not null);
}
