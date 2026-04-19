using Amolenk.Admitto.Module.Email.Domain.ValueObjects;

namespace Amolenk.Admitto.Module.Email.Application.UseCases.EventEmailSettings.GetEventEmailSettings;

/// <summary>
/// Read model for an event's email settings. The password value is never exposed; only a flag
/// indicating whether one is stored.
/// </summary>
public sealed record EventEmailSettingsDto(
    string SmtpHost,
    int SmtpPort,
    string FromAddress,
    EmailAuthMode AuthMode,
    string? Username,
    bool HasPassword,
    uint Version);
