using Amolenk.Admitto.Module.Registrations.Application.Persistence;
using Amolenk.Admitto.Module.Registrations.Domain.Entities;
using Amolenk.Admitto.Module.Shared.Application.Messaging;
using Amolenk.Admitto.Module.Shared.Kernel.ErrorHandling;
using Microsoft.EntityFrameworkCore;

namespace Amolenk.Admitto.Module.Registrations.Application.UseCases.CouponManagement.RevokeCoupon;

internal sealed class RevokeCouponHandler(IRegistrationsWriteStore writeStore)
    : ICommandHandler<RevokeCouponCommand>
{
    public async ValueTask HandleAsync(
        RevokeCouponCommand command,
        CancellationToken cancellationToken)
    {
        var coupon = await writeStore.Coupons
            .FirstOrDefaultAsync(
                c => c.Id == command.CouponId && c.EventId == command.EventId,
                cancellationToken);

        if (coupon is null)
        {
            throw new BusinessRuleViolationException(
                NotFoundError.Create<Coupon>(command.CouponId.Value));
        }

        coupon.Revoke();
    }
}
