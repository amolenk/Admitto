namespace Amolenk.Admitto.Core.Registrations.Application.UseCases.Registrations.RegisterAttendeeWithCoupon.PartnerApi;

public sealed record RegisterAttendeeWithCouponHttpRequest(
    Guid CouponCode,
    string Email,
    string FirstName,
    string LastName,
    Guid[] TicketTypeIds,
    Dictionary<string, string>? AdditionalDetails = null);
