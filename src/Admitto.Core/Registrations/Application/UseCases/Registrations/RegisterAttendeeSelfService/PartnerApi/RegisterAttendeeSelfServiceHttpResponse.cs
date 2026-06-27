namespace Amolenk.Admitto.Core.Registrations.Application.UseCases.Registrations.RegisterAttendeeSelfService.PartnerApi;

public sealed record RegisterAttendeeSelfServiceHttpResponse(
    Guid? RegistrationId,
    Guid[] RegisteredTicketTypeIds,
    Guid[] WaitlistedTicketTypeIds);
