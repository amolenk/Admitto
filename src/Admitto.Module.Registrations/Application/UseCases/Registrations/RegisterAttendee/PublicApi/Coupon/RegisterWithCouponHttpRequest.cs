namespace Amolenk.Admitto.Module.Registrations.Application.UseCases.Registrations.RegisterAttendee.PublicApi.Coupon;

public sealed record RegisterWithCouponHttpRequest(
    string CouponCode,
    string Email,
    string FirstName,
    string LastName,
    string[] TicketTypeSlugs,
    Dictionary<string, string>? AdditionalDetails = null);
