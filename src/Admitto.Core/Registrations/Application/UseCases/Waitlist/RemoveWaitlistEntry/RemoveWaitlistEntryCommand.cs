using Amolenk.Admitto.Core.Shared.Application.Messaging;

namespace Amolenk.Admitto.Core.Registrations.Application.UseCases.Waitlist.RemoveWaitlistEntry;

internal sealed record RemoveWaitlistEntryCommand(
    Guid EventId,
    Guid TicketTypeId,
    Guid EntryId) : Command;
