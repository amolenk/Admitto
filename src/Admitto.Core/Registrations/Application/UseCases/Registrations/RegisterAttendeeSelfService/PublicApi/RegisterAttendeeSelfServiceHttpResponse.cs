namespace Amolenk.Admitto.Core.Registrations.Application.UseCases.Registrations.RegisterAttendeeSelfService.PublicApi;

public sealed record RegisterAttendeeSelfServiceHttpResponse(
    Guid? RegistrationId,
    Guid[] RegisteredTicketTypeIds,
    Guid[] WaitlistedTicketTypeIds);
