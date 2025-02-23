using Amolenk.Admitto.Domain.ValueObjects;

namespace Amolenk.Admitto.Application.Features.Attendees.RegisterAttendee;

public record ReserveTicketsCommand(Guid RegistrationId, Guid AttendeeId, Guid TicketedEventId, TicketOrder TicketOrder) 
    : ICommand
{
    public Guid CommandId { get; init; } = Guid.NewGuid();
}
