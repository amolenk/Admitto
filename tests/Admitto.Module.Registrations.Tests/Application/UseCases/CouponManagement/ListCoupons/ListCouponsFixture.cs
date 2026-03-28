using Amolenk.Admitto.Module.Registrations.Domain.Tests.Builders;
using Amolenk.Admitto.Module.Registrations.Domain.ValueObjects;
using Amolenk.Admitto.Module.Registrations.Tests.Application.Infrastructure.Hosting;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Module.Registrations.Tests.Application.UseCases.CouponManagement.ListCoupons;

internal sealed class ListCouponsFixture
{
    private bool _seedCoupons;

    public TicketedEventId EventId { get; } = TicketedEventId.New();
    public TicketTypeId TicketTypeId { get; } = TicketTypeId.New();

    private ListCouponsFixture()
    {
    }

    public static ListCouponsFixture EmptyList() => new();

    public static ListCouponsFixture WithCoupons() => new()
    {
        _seedCoupons = true
    };

    public async ValueTask SetupAsync(IntegrationTestEnvironment environment)
    {
        if (!_seedCoupons)
        {
            return;
        }

        // Seed an active coupon.
        var activeCoupon = new CouponBuilder()
            .WithEventId(EventId)
            .WithEmail(EmailAddress.From("active@example.com"))
            .WithRequestedTicketTypeIds(TicketTypeId)
            .WithAvailableTicketTypes(new TicketTypeInfo(TicketTypeId, IsCancelled: false))
            .WithExpiresAt(DateTimeOffset.UtcNow.AddDays(30))
            .Build();

        // Seed a revoked coupon.
        var revokedCoupon = new CouponBuilder()
            .WithEventId(EventId)
            .WithEmail(EmailAddress.From("revoked@example.com"))
            .WithRequestedTicketTypeIds(TicketTypeId)
            .WithAvailableTicketTypes(new TicketTypeInfo(TicketTypeId, IsCancelled: false))
            .WithExpiresAt(DateTimeOffset.UtcNow.AddDays(30))
            .Build();
        revokedCoupon.Revoke();

        await environment.Database.SeedAsync(dbContext =>
        {
            dbContext.Coupons.Add(activeCoupon);
            dbContext.Coupons.Add(revokedCoupon);
        });
    }
}
