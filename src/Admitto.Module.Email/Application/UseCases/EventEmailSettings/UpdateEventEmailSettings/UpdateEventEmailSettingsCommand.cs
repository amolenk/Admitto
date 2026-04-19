using Amolenk.Admitto.Module.Email.Domain.ValueObjects;
using Amolenk.Admitto.Module.Shared.Application.Messaging;

namespace Amolenk.Admitto.Module.Email.Application.UseCases.EventEmailSettings.UpdateEventEmailSettings;

internal sealed record UpdateEventEmailSettingsCommand(
    Guid TicketedEventId,
    string? SmtpHost,
    int? SmtpPort,
    string? FromAddress,
    EmailAuthMode? AuthMode,
    string? Username,
    string? Password,
    uint ExpectedVersion) : Command;
