namespace Amolenk.Admitto.Core.Registrations.Application.UseCases.Registrations.RegisterAttendee.PartnerApi;

public sealed record RegisterAttendeeHttpRequest(
    string Email,
    string FirstName,
    string LastName,
    Guid[] RegisterTicketTypeIds,
    Guid[] WaitlistTicketTypeIds,
    Dictionary<string, string>? AdditionalDetails = null,
    Guid? CouponCode = null);
