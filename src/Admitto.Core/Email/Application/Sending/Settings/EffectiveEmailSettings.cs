using Amolenk.Admitto.Core.Email.Domain.ValueObjects;

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
    string FromDisplayName,
    EmailAuthMode AuthMode,
    string? Username,
    string? Password)
{
    /// <summary>
    /// Returns whether the deployment-global transport settings are complete enough
    /// to attempt SMTP delivery.
    /// </summary>
    public bool IsValid() =>
        !string.IsNullOrWhiteSpace(SmtpHost.Value)
        && SmtpPort.Value is >= Port.MinValue and <= Port.MaxValue
        && !(SmtpSsl && SmtpStartTls)
        && !string.IsNullOrWhiteSpace(FromAddress.Value)
        && (AuthMode == EmailAuthMode.None
            || AuthMode == EmailAuthMode.Basic
                && !string.IsNullOrWhiteSpace(Username)
                && !string.IsNullOrWhiteSpace(Password));
}
