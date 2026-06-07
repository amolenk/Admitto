using Amolenk.Admitto.Core.Shared.Application.Messaging;

namespace Amolenk.Admitto.Core.Email.Application.UseCases.EmailSettings.DeleteEmailSettings;

internal sealed record DeleteEmailSettingsCommand(
    Guid TeamId,
    Guid? TicketedEventId,
    uint ExpectedVersion) : Command;
