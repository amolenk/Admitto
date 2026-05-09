using Amolenk.Admitto.Core.Module.Registrations.Application.Persistence;
using Amolenk.Admitto.Core.Module.Registrations.Domain.Entities;
using Amolenk.Admitto.Core.Shared.Application.Messaging;
using Amolenk.Admitto.Core.Shared.Kernel.ErrorHandling;
using Microsoft.EntityFrameworkCore;

namespace Amolenk.Admitto.Core.Module.Registrations.Application.UseCases.CouponManagement.GetCouponDetails;

internal sealed class GetCouponDetailsHandler(IRegistrationsWriteStore writeStore)
    : IQueryHandler<GetCouponDetailsQuery, CouponDetailsDto>
{
    public async ValueTask<CouponDetailsDto> HandleAsync(
        GetCouponDetailsQuery query,
        CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;

        var coupon = await writeStore.Coupons
            .FirstOrDefaultAsync(
                c => c.Id == query.CouponId && c.EventId == query.EventId,
                cancellationToken);

        if (coupon is null)
        {
            throw new BusinessRuleViolationException(
                NotFoundError.Create<Coupon>(query.CouponId.Value));
        }

        return new CouponDetailsDto(
            coupon.Id.Value,
            coupon.Code.Value,
            coupon.Email.Value,
            coupon.GetStatus(now),
            coupon.AllowedTicketTypeSlugs.ToArray(),
            coupon.ExpiresAt,
            coupon.BypassRegistrationWindow,
            coupon.RedeemedAt,
            coupon.RevokedAt,
            coupon.CreatedAt);
    }
}
