using Amolenk.Admitto.Core.Shared.Application.Messaging;

namespace Amolenk.Admitto.Core.Email.Application.UseCases.EmailSettings.SendTestEmail;

internal sealed record SendTestEmailCommand(
    Guid TeamId,
    Guid? TicketedEventId,
    string Recipient) : Command;
