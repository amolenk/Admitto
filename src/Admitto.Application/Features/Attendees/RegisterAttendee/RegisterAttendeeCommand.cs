namespace Amolenk.Admitto.Application.Features.Attendees.RegisterAttendee;

public record RegisterAttendeeCommand(string Email, Guid TicketedEventId, IEnumerable<Guid> TicketTypes) : ICommand
{
    public Guid CommandId { get; init; } = Guid.NewGuid();
}
