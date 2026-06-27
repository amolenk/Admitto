using Amolenk.Admitto.Core.Email.Domain.ValueObjects;
using Microsoft.Extensions.Options;

namespace Amolenk.Admitto.Core.Email.Application.Sending.Settings;

internal interface ISystemEmailSettingsResolver
{
    EffectiveEmailSettings? Resolve();
}

internal sealed class SystemEmailSettingsResolver(IOptions<SystemEmailOptions> options) : ISystemEmailSettingsResolver
{
    public EffectiveEmailSettings? Resolve()
    {
        var value = options.Value;
        if (string.IsNullOrWhiteSpace(value.SmtpHost) || string.IsNullOrWhiteSpace(value.FromAddress))
            return null;

        var authMode = Enum.TryParse<EmailAuthMode>(value.AuthMode, ignoreCase: true, out var parsed)
            ? parsed
            : EmailAuthMode.None;

        return new EffectiveEmailSettings(
            Hostname.From(value.SmtpHost),
            Port.From(value.SmtpPort),
            EmailAddress.From(value.FromAddress),
            authMode,
            authMode == EmailAuthMode.Basic ? value.Username : null,
            authMode == EmailAuthMode.Basic ? value.Password : null,
            EmailAccentColor.From("#2563eb"),
            EmailFontFamily.From("Inter, sans-serif"));
    }
}
