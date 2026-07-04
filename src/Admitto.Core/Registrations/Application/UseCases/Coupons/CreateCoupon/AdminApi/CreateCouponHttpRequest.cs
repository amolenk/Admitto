namespace Amolenk.Admitto.Core.Registrations.Application.UseCases.Coupons.CreateCoupon.AdminApi;

public sealed record CreateCouponHttpRequest(
    string Email,
    Guid[] AllowedTicketTypeIds,
    DateTimeOffset ExpiresAt,
    bool BypassRegistrationWindow = false)
{
    internal CreateCouponCommand ToCommand(Guid teamId, Guid eventId) => new(
        teamId,
        eventId,
        Email,
        AllowedTicketTypeIds,
        ExpiresAt,
        BypassRegistrationWindow);
}
