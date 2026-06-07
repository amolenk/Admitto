using Amolenk.Admitto.Core.Email.Domain.ValueObjects;
using Amolenk.Admitto.Core.Shared.Application.Messaging;

namespace Amolenk.Admitto.Core.Email.Application.UseCases.EmailSettings.UpdateEmailSettings;

internal sealed record UpdateEmailSettingsCommand(
    Guid TeamId,
    Guid? TicketedEventId,
    string? SmtpHost,
    int? SmtpPort,
    string? FromAddress,
    EmailAuthMode? AuthMode,
    string? Username,
    string? Password,
    uint ExpectedVersion) : Command;
