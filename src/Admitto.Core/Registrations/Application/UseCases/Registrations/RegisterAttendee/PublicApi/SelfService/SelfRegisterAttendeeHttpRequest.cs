namespace Amolenk.Admitto.Core.Registrations.Application.UseCases.Registrations.RegisterAttendee.PublicApi.SelfService;

public sealed record SelfRegisterAttendeeHttpRequest(
    string FirstName,
    string LastName,
    Guid[] TicketTypeIds,
    Dictionary<string, string>? AdditionalDetails = null);

