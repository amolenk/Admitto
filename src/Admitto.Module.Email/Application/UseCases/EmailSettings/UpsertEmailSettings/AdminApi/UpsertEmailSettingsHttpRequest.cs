using Amolenk.Admitto.Module.Email.Application.UseCases.EmailSettings.CreateEmailSettings;
using Amolenk.Admitto.Module.Email.Application.UseCases.EmailSettings.UpdateEmailSettings;
using Amolenk.Admitto.Module.Email.Domain.ValueObjects;

namespace Amolenk.Admitto.Module.Email.Application.UseCases.EmailSettings.UpsertEmailSettings.AdminApi;

/// <summary>
/// PUT request body for upserting email settings. When <see cref="Version"/> is omitted the
/// endpoint creates a new record; when supplied it updates the existing record using optimistic
/// concurrency. When <see cref="Password"/> is omitted on update, the existing encrypted password
/// is preserved.
/// </summary>
public sealed record UpsertEmailSettingsHttpRequest(
    string SmtpHost,
    int SmtpPort,
    string FromAddress,
    EmailAuthMode AuthMode,
    string? Username,
    string? Password,
    uint? Version)
{
    internal CreateEmailSettingsCommand ToCreateCommand(EmailSettingsScope scope, Guid scopeId) =>
        new(scope, scopeId, SmtpHost, SmtpPort, FromAddress, AuthMode, Username, Password);

    internal UpdateEmailSettingsCommand ToUpdateCommand(EmailSettingsScope scope, Guid scopeId, uint expectedVersion) =>
        new(scope, scopeId, SmtpHost, SmtpPort, FromAddress, AuthMode, Username, Password, expectedVersion);
}
