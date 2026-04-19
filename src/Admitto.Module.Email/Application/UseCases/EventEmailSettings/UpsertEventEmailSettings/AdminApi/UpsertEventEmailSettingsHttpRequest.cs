using Amolenk.Admitto.Module.Email.Domain.ValueObjects;
using Amolenk.Admitto.Module.Email.Application.UseCases.EventEmailSettings.CreateEventEmailSettings;
using Amolenk.Admitto.Module.Email.Application.UseCases.EventEmailSettings.UpdateEventEmailSettings;

namespace Amolenk.Admitto.Module.Email.Application.UseCases.EventEmailSettings.UpsertEventEmailSettings.AdminApi;

/// <summary>
/// PUT request body for upserting event email settings. When <see cref="Version"/> is omitted the
/// endpoint creates a new record; when supplied it updates the existing record using optimistic
/// concurrency. When <see cref="Password"/> is omitted on update, the existing encrypted password
/// is preserved.
/// </summary>
public sealed record UpsertEventEmailSettingsHttpRequest(
    string SmtpHost,
    int SmtpPort,
    string FromAddress,
    EmailAuthMode AuthMode,
    string? Username,
    string? Password,
    uint? Version)
{
    internal CreateEventEmailSettingsCommand ToCreateCommand(Guid ticketedEventId) =>
        new(
            ticketedEventId,
            SmtpHost,
            SmtpPort,
            FromAddress,
            AuthMode,
            Username,
            Password);

    internal UpdateEventEmailSettingsCommand ToUpdateCommand(Guid ticketedEventId, uint expectedVersion) =>
        new(
            ticketedEventId,
            SmtpHost,
            SmtpPort,
            FromAddress,
            AuthMode,
            Username,
            Password,
            expectedVersion);
}
