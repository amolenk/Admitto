namespace Amolenk.Admitto.Module.Registrations.Application.UseCases.Registrations.RegisterWithCoupon.PublicApi;

public sealed record RegisterWithCouponHttpRequest(
    string CouponCode,
    string Email,
    string[] TicketTypeSlugs,
    Dictionary<string, string>? AdditionalDetails = null);
