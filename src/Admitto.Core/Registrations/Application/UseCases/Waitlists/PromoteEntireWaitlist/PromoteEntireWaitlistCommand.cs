using Amolenk.Admitto.Core.Shared.Application.Messaging;

namespace Amolenk.Admitto.Core.Registrations.Application.UseCases.Waitlists.PromoteEntireWaitlist;

internal sealed record PromoteEntireWaitlistCommand(
    Guid EventId,
    Guid TeamId,
    Guid TicketTypeId) : Command;
