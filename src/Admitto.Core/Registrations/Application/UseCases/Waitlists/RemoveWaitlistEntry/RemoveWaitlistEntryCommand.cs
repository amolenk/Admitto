using Amolenk.Admitto.Core.Shared.Application.Messaging;

namespace Amolenk.Admitto.Core.Registrations.Application.UseCases.Waitlists.RemoveWaitlistEntry;

internal sealed record RemoveWaitlistEntryCommand(
    Guid EventId,
    Guid TeamId,
    Guid TicketTypeId,
    Guid EntryId) : Command;
