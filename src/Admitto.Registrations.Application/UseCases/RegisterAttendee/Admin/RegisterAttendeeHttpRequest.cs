namespace Amolenk.Admitto.Registrations.Application.UseCases.RegisterAttendee.Admin;

public record RegisterAttendeeHttpRequest(
    string FirstName,
    string LastName,
    string Email,
    Guid[] TicketTypeIds);