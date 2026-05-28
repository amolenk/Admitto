using Amolenk.Admitto.Core.Registrations.Application.Persistence;
using Amolenk.Admitto.Core.Shared.Application.Messaging;
using Amolenk.Admitto.Core.Shared.Application.Persistence;

namespace Amolenk.Admitto.Core.Registrations.Application.UseCases.Coupons.GetPublicCouponDetails;

internal sealed class GetPublicCouponDetailsHandler(IRegistrationsWriteStore writeStore)
    : IQueryHandler<GetPublicCouponDetailsQuery, PublicCouponDetailsDto>
{
    public async ValueTask<PublicCouponDetailsDto> HandleAsync(
        GetPublicCouponDetailsQuery query,
        CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;

        var coupon = await writeStore.Coupons.GetUntrackedAsync(
            c => c.Code == query.Code && c.EventId == query.EventId,
            cancellationToken);

        var catalog = await writeStore.TicketCatalogs.GetUntrackedAsync(
            tc => tc.Id == query.EventId,
            cancellationToken);

        var allowedTicketTypes = coupon.AllowedTicketTypeIds
            .Select(id =>
            {
                var ticketType = catalog.GetTicketType(id);
                return new AllowedTicketTypeDto(id.Value, ticketType?.Name.Value ?? string.Empty);
            })
            .ToList();

        return new PublicCouponDetailsDto(
            coupon.GetStatus(now),
            allowedTicketTypes,
            coupon.ExpiresAt);
    }
}
