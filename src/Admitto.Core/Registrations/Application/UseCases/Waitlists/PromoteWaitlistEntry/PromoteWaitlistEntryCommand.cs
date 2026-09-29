using Amolenk.Admitto.Core.Shared.Application.Messaging;

namespace Amolenk.Admitto.Core.Registrations.Application.UseCases.Waitlists.PromoteWaitlistEntry;

internal sealed record PromoteWaitlistEntryCommand(
    Guid EventId,
    Guid TeamId,
    Guid TicketTypeId,
    Guid EntryId) : Command<Guid>;
