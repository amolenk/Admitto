namespace Amolenk.Admitto.Core.Registrations.Application.UseCases.Registrations.UpdatePartnerRegistration.PartnerApi;

public sealed record UpdatePartnerRegistrationHttpRequest(
    string FirstName,
    string LastName,
    Guid[]? RegisterTicketTypeIds,
    Guid[]? WaitlistTicketTypeIds,
    Dictionary<string, string>? AdditionalDetails = null,
    Guid? WaitlistCouponCode = null);
