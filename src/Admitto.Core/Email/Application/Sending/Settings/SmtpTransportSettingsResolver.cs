using Amolenk.Admitto.Core.Email.Domain.ValueObjects;
using Microsoft.Extensions.Options;
using Vogen;

namespace Amolenk.Admitto.Core.Email.Application.Sending.Settings;

/// <summary>
/// Email-module-internal contract for resolving deployment SMTP settings.
/// </summary>
internal interface ISmtpTransportSettingsResolver
{
    ValueTask<SmtpTransportSettings?> ResolveAsync(
        CancellationToken cancellationToken = default);
}

internal sealed class SmtpTransportSettingsResolver(
    IOptions<SystemEmailOptions> options) : ISmtpTransportSettingsResolver
{
    public ValueTask<SmtpTransportSettings?> ResolveAsync(
        CancellationToken cancellationToken = default)
    {
        var value = options.Value;
        if (string.IsNullOrWhiteSpace(value.SmtpHost) || string.IsNullOrWhiteSpace(value.FromAddress))
            return ValueTask.FromResult<SmtpTransportSettings?>(null);

        var authMode = ResolveAuthMode(value.AuthMode);
        if (authMode is null)
            return ValueTask.FromResult<SmtpTransportSettings?>(null);

        try
        {
            var settings = new SmtpTransportSettings(
                Hostname.From(value.SmtpHost),
                Port.From(value.SmtpPort),
                value.SmtpSsl,
                value.SmtpStartTls,
                EmailAddress.From(value.FromAddress),
                value.FromDisplayName,
                authMode.Value,
                authMode == EmailAuthMode.Basic ? value.Username : null,
                authMode == EmailAuthMode.Basic ? value.Password : null);

            return ValueTask.FromResult<SmtpTransportSettings?>(settings.IsValid() ? settings : null);
        }
        catch (ValueObjectValidationException)
        {
            // Invalid value-object configuration is an expected configuration failure.
            return ValueTask.FromResult<SmtpTransportSettings?>(null);
        }
    }

    private static EmailAuthMode? ResolveAuthMode(string? value)
    {
        if (bool.TryParse(value, out var enabled))
            return enabled ? EmailAuthMode.Basic : EmailAuthMode.None;

        return Enum.TryParse<EmailAuthMode>(value, ignoreCase: true, out var parsed)
            && parsed is (EmailAuthMode.None or EmailAuthMode.Basic)
            ? parsed
            : null;
    }
}
