namespace Amolenk.Admitto.Core.Registrations.Application.UseCases.Registrations.RegisterAttendeeSelfService.PartnerApi;

public sealed record RegisterAttendeeSelfServiceHttpRequest(
    string Email,
    string FirstName,
    string LastName,
    Guid[] RegisterTicketTypeIds,
    Guid[] WaitlistTicketTypeIds,
    Dictionary<string, string>? AdditionalDetails = null);
