namespace Amolenk.Admitto.Core.Registrations.Application.UseCases.Registrations.RegisterAttendeeSelfService.PublicApi;

public sealed record RegisterAttendeeSelfServiceHttpRequest(
    string FirstName,
    string LastName,
    Guid[] TicketTypeIds,
    Dictionary<string, string>? AdditionalDetails = null);
