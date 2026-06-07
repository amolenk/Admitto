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
    uint? Version)
{
    internal CreateEmailSettingsCommand ToCreateCommand(Guid teamId, Guid? ticketedEventId) =>
        new(teamId, ticketedEventId, SmtpHost, SmtpPort, FromAddress, AuthMode, Username, Password);

    internal UpdateEmailSettingsCommand ToUpdateCommand(Guid teamId, Guid? ticketedEventId, uint expectedVersion) =>
        new(teamId, ticketedEventId, SmtpHost, SmtpPort, FromAddress, AuthMode, Username, Password, expectedVersion);
}
