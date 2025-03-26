namespace Amolenk.Admitto.Application.UseCases.Attendees.RegisterAttendee;

public record RegisterAttendeeCommand(Guid TicketedEventId, string Email, string FirstName, string LastName,
    string OrganizationName, IEnumerable<Guid> TicketTypes) : ICommand
{
    public Guid Id { get; init; } = Guid.NewGuid();
}
