using Amolenk.Admitto.Module.Registrations.Application.Persistence;
using Amolenk.Admitto.Module.Shared.Application.Messaging;
using Microsoft.EntityFrameworkCore;

namespace Amolenk.Admitto.Module.Registrations.Application.UseCases.CouponManagement.ListCoupons;

internal sealed class ListCouponsHandler(IRegistrationsWriteStore writeStore)
    : IQueryHandler<ListCouponsQuery, ListCouponsResult>
{
    public async ValueTask<ListCouponsResult> HandleAsync(
        ListCouponsQuery query,
        CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;

        var coupons = await writeStore.Coupons
            .Where(c => c.EventId == query.EventId)
            .OrderByDescending(c => c.CreatedAt)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var summaries = coupons
            .Select(c => new CouponSummaryDto(
                c.Id.Value,
                c.Email.Value,
                c.GetStatus(now),
                c.AllowedTicketTypeIds.Select(id => id.Value).ToArray(),
                c.ExpiresAt,
                c.CreatedAt))
            .ToList();

        return new ListCouponsResult(summaries);
    }
}
