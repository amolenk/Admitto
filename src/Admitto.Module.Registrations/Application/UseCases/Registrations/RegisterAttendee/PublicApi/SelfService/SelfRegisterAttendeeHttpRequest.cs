namespace Amolenk.Admitto.Module.Registrations.Application.UseCases.Registrations.RegisterAttendee.PublicApi.SelfService;

public sealed record SelfRegisterAttendeeHttpRequest(
    string Email,
    string FirstName,
    string LastName,
    string[] TicketTypeSlugs,
    string? EmailVerificationToken = null,
    Dictionary<string, string>? AdditionalDetails = null);
