namespace Amolenk.Admitto.Module.Registrations.Application.UseCases.Registrations.RegisterAttendee.PublicApi.SelfService;

public sealed record SelfRegisterAttendeeHttpRequest(
    string FirstName,
    string LastName,
    string[] TicketTypeSlugs,
    Dictionary<string, string>? AdditionalDetails = null);

