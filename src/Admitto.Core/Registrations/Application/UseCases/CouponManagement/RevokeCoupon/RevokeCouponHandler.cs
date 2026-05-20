using Amolenk.Admitto.Core.Registrations.Application.Persistence;
using Amolenk.Admitto.Core.Registrations.Domain.ValueObjects;
using Amolenk.Admitto.Core.Shared.Application.Messaging;
using Amolenk.Admitto.Core.Shared.Application.Persistence;

namespace Amolenk.Admitto.Core.Registrations.Application.UseCases.CouponManagement.RevokeCoupon;

internal sealed class RevokeCouponHandler(IRegistrationsWriteStore writeStore)
    : ICommandHandler<RevokeCouponCommand>
{
    public async ValueTask HandleAsync(
        RevokeCouponCommand command,
        CancellationToken cancellationToken)
    {
        TicketedEventId eventId = TicketedEventId.From(command.EventId);
        CouponId couponId = CouponId.From(command.CouponId);

        var coupon = await writeStore.Coupons.GetAsync(
                 c => c.Id == couponId && c.EventId == eventId,
                 cancellationToken);

        coupon.Revoke();
    }
}
