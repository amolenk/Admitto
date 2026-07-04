namespace Amolenk.Admitto.Core.Registrations.Application.UseCases.Registrations.UpdatePartnerRegistration.PartnerApi;

public sealed record UpdatePartnerRegistrationHttpRequest(
    string FirstName,
    string LastName,
    Guid[]? TicketTypeIds,
    Dictionary<string, string>? AdditionalDetails = null,
    Guid? WaitlistCouponCode = null);
