namespace Amolenk.Admitto.Module.Registrations.Application.UseCases.Registrations.SelfRegisterAttendee.PublicApi;

public sealed record SelfRegisterAttendeeHttpRequest(
    string Email,
    string[] TicketTypeSlugs,
    Dictionary<string, string>? AdditionalDetails = null);
