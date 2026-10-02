namespace Amolenk.Admitto.Core.Registrations.Application.UseCases.Registrations.RegisterAttendee.PartnerApi;

public sealed record RegisterAttendeeHttpResponse(
    Guid RegistrationId,
    Guid[] RegisteredTicketTypeIds,
    Guid[] WaitlistedTicketTypeIds);
