using Amolenk.Admitto.Core.Email.Application.UseCases.EmailSettings.CreateEmailSettings;
using Amolenk.Admitto.Core.Email.Application.UseCases.EmailSettings.UpdateEmailSettings;
using Amolenk.Admitto.Core.Email.Domain.ValueObjects;

namespace Amolenk.Admitto.Core.Email.Application.UseCases.EmailSettings.UpsertEmailSettings.AdminApi;

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
    string? AccentColor,
    string? FontFamily,
    uint? Version)
{
    internal CreateEmailSettingsCommand ToCreateCommand(Guid teamId) =>
        new(teamId, SmtpHost, SmtpPort, FromAddress, AuthMode, Username, Password, AccentColor, FontFamily);

    internal UpdateEmailSettingsCommand ToUpdateCommand(Guid teamId, uint expectedVersion) =>
        new(teamId, SmtpHost, SmtpPort, FromAddress, AuthMode, Username, Password, AccentColor, FontFamily, expectedVersion);
}
