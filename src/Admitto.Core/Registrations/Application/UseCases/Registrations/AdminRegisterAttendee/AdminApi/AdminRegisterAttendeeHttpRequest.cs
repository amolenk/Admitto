namespace Amolenk.Admitto.Core.Registrations.Application.UseCases.Registrations.AdminRegisterAttendee.AdminApi;

public sealed record AdminRegisterAttendeeHttpRequest(
    string Email,
    string FirstName,
    string LastName,
    Guid[] TicketTypeIds,
    Dictionary<string, string>? AdditionalDetails = null);
