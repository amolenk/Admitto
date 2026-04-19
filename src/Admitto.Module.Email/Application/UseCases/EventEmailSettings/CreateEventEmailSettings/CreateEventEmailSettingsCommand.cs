using Amolenk.Admitto.Module.Email.Domain.ValueObjects;
using Amolenk.Admitto.Module.Shared.Application.Messaging;

namespace Amolenk.Admitto.Module.Email.Application.UseCases.EventEmailSettings.CreateEventEmailSettings;

internal sealed record CreateEventEmailSettingsCommand(
    Guid TicketedEventId,
    string SmtpHost,
    int SmtpPort,
    string FromAddress,
    EmailAuthMode AuthMode,
    string? Username,
    string? Password) : Command;
