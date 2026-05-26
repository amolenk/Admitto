using Amolenk.Admitto.Core.Shared.Application.Messaging;

namespace Amolenk.Admitto.Core.Registrations.Application.UseCases.Waitlist.LeaveWaitlist;

internal sealed record LeaveWaitlistCommand(
    Guid EventId,
    Guid TicketTypeId,
    string Email) : Command;
