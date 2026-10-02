using Amolenk.Admitto.Core.Shared.Application.Messaging;

namespace Amolenk.Admitto.Core.Registrations.Application.UseCases.Waitlists.DisableWaitlist;

internal sealed record DisableWaitlistCommand(
    Guid EventId,
    Guid TeamId,
    Guid TicketTypeId,
    int FreedSlots) : Command;
