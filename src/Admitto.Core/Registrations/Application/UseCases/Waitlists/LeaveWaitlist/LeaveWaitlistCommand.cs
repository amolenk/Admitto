using Amolenk.Admitto.Core.Shared.Application.Messaging;

namespace Amolenk.Admitto.Core.Registrations.Application.UseCases.Waitlists.LeaveWaitlist;

internal sealed record LeaveWaitlistCommand(
    Guid TeamId,
    Guid EventId,
    Guid TicketTypeId,
    string Email) : Command;
