namespace Amolenk.Admitto.Module.Registrations.Application.UseCases.Registrations.AdminRegisterAttendee.AdminApi;

public sealed record AdminRegisterAttendeeHttpRequest(
    string Email,
    string[] TicketTypeSlugs,
    Dictionary<string, string>? AdditionalDetails = null);
