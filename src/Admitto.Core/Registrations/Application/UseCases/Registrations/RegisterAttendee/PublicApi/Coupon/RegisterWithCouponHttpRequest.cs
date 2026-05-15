namespace Amolenk.Admitto.Core.Registrations.Application.UseCases.Registrations.RegisterAttendee.PublicApi.Coupon;

public sealed record RegisterWithCouponHttpRequest(
    string CouponCode,
    string Email,
    string FirstName,
    string LastName,
    Guid[] TicketTypeIds,
    Dictionary<string, string>? AdditionalDetails = null);
