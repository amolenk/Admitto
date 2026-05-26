using Amolenk.Admitto.Core.Shared.Application.Messaging;

namespace Amolenk.Admitto.Core.Registrations.Application.UseCases.Waitlist.JoinWaitlist;

internal sealed record JoinWaitlistCommand(
    Guid TeamId,
    Guid EventId,
    Guid TicketTypeId,
    string Token) : Command;
