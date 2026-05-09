namespace Amolenk.Admitto.Core.Registrations.Application.UseCases.CouponManagement.CreateCoupon.AdminApi;

public sealed record CreateCouponHttpRequest(
    string Email,
    string[] AllowedTicketTypeSlugs,
    DateTimeOffset ExpiresAt,
    bool BypassRegistrationWindow = false)
{
    internal CreateCouponCommand ToCommand(Guid eventId) => new(
        eventId,
        Email,
        AllowedTicketTypeSlugs,
        ExpiresAt,
        BypassRegistrationWindow);
}
