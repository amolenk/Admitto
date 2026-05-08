using Amolenk.Admitto.Module.Registrations.Application.Persistence;
using Amolenk.Admitto.Module.Registrations.Domain.Entities;
using Amolenk.Admitto.Module.Registrations.Domain.ValueObjects;
using Amolenk.Admitto.Module.Shared.Application.Messaging;
using Amolenk.Admitto.Module.Shared.Kernel.ErrorHandling;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace Amolenk.Admitto.Module.Registrations.Application.UseCases.CouponManagement.RevokeCoupon;

internal sealed class RevokeCouponHandler(IRegistrationsWriteStore writeStore)
    : ICommandHandler<RevokeCouponCommand>
{
    public async ValueTask HandleAsync(
        RevokeCouponCommand command,
        CancellationToken cancellationToken)
    {
        TicketedEventId eventId = TicketedEventId.From(command.EventId);
        CouponId couponId = CouponId.From(command.CouponId);

        var coupon = await writeStore.Coupons
            .FirstOrDefaultAsync(
                c => c.Id == couponId && c.EventId == eventId,
                cancellationToken);

        if (coupon is null)
        {
            throw new BusinessRuleViolationException(
                NotFoundError.Create<Coupon>(couponId.Value));
        }

        coupon.Revoke();
    }
}
