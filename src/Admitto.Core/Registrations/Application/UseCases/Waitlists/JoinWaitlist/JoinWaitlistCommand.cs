using Amolenk.Admitto.Core.Shared.Application.Messaging;

namespace Amolenk.Admitto.Core.Registrations.Application.UseCases.Waitlists.JoinWaitlist;

internal sealed record JoinWaitlistCommand(
    Guid TeamId,
    Guid EventId,
    Guid TicketTypeId,
    string Email) : Command;
