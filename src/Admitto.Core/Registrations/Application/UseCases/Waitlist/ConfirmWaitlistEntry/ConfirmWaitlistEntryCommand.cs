using Amolenk.Admitto.Core.Shared.Application.Messaging;

namespace Amolenk.Admitto.Core.Registrations.Application.UseCases.Waitlist.ConfirmWaitlistEntry;

internal sealed record ConfirmWaitlistEntryCommand(
    Guid EventId,
    Guid TicketTypeId,
    string Token) : Command;
