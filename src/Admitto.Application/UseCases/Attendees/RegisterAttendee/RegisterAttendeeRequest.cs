namespace Amolenk.Admitto.Application.UseCases.Attendees.RegisterAttendee;

public record RegisterAttendeeRequest(
    string Email,
    string FirstName,
    string LastName,
    string OrganizationName,
    IEnumerable<Guid> TicketTypes);
