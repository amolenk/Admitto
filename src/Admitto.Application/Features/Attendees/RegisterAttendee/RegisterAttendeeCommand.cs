namespace Amolenk.Admitto.Application.Features.Attendees.RegisterAttendee;

public record RegisterAttendeeCommand(Guid TicketedEventId, string Email, string FirstName, string LastName,
    string OrganizationName, IEnumerable<Guid> TicketTypes) : ICommand
{
    public Guid CommandId { get; init; } = Guid.NewGuid();
}
