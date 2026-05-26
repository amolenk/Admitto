using Amolenk.Admitto.Core.Registrations.Application.Persistence;
using Amolenk.Admitto.Core.Shared.Application.Messaging;
using Amolenk.Admitto.Core.Shared.Application.Persistence;

namespace Amolenk.Admitto.Core.Registrations.Application.UseCases.CouponManagement.GetCouponDetails;

internal sealed class GetCouponDetailsHandler(IRegistrationsWriteStore writeStore)
    : IQueryHandler<GetCouponDetailsQuery, CouponDetailsDto>
{
    public async ValueTask<CouponDetailsDto> HandleAsync(
        GetCouponDetailsQuery query,
        CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;

        var coupon = await writeStore.Coupons.GetAsync(
                 c => c.Id == query.CouponId && c.EventId == query.EventId,
                 cancellationToken);

        return new CouponDetailsDto(
            coupon.Id.Value,
            coupon.Code.Value,
            coupon.Email.Value,
            coupon.GetStatus(now),
            coupon.Source,
            coupon.AllowedTicketTypeIds.Select(id => id.Value).ToArray(),
            coupon.ExpiresAt,
            coupon.BypassRegistrationWindow,
            coupon.RedeemedAt,
            coupon.RevokedAt,
            coupon.CreatedAt);
    }
}
