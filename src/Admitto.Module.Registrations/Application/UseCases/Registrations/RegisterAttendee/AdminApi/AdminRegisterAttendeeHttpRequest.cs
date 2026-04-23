namespace Amolenk.Admitto.Module.Registrations.Application.UseCases.Registrations.RegisterAttendee.AdminApi;

public sealed record AdminRegisterAttendeeHttpRequest(
    string Email,
    string[] TicketTypeSlugs,
    Dictionary<string, string>? AdditionalDetails = null);
