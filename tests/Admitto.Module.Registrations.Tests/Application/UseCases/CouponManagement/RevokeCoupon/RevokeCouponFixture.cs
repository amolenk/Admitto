using Amolenk.Admitto.Module.Registrations.Domain.Tests.Builders;
using Amolenk.Admitto.Module.Registrations.Domain.ValueObjects;
using Amolenk.Admitto.Module.Registrations.Tests.Application.Infrastructure.Hosting;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Module.Registrations.Tests.Application.UseCases.CouponManagement.RevokeCoupon;

internal sealed class RevokeCouponFixture
{
    private bool _seedActiveCoupon;
    private bool _seedRedeemedCoupon;
    private bool _seedRevokedCoupon;
    private bool _seedExpiredCoupon;

    public TicketedEventId EventId { get; } = TicketedEventId.New();
    public string TicketTypeSlug { get; } = "general-admission";
    public CouponId CouponId { get; private set; } = CouponId.New();

    private RevokeCouponFixture()
    {
    }

    public static RevokeCouponFixture ActiveCoupon() => new()
    {
        _seedActiveCoupon = true
    };

    public static RevokeCouponFixture RedeemedCoupon() => new()
    {
        _seedRedeemedCoupon = true
    };

    public static RevokeCouponFixture AlreadyRevokedCoupon() => new()
    {
        _seedRevokedCoupon = true
    };

    public static RevokeCouponFixture ExpiredCoupon() => new()
    {
        _seedExpiredCoupon = true
    };

    public static RevokeCouponFixture NoCoupon() => new();

    public async ValueTask SetupAsync(IntegrationTestEnvironment environment)
    {
        if (!_seedActiveCoupon && !_seedRedeemedCoupon && !_seedRevokedCoupon && !_seedExpiredCoupon)
        {
            return;
        }

        var builder = new CouponBuilder()
            .WithEventId(EventId)
            .WithRequestedTicketTypeSlugs(TicketTypeSlug)
            .WithAvailableTicketTypes(new TicketTypeInfo(TicketTypeSlug, IsCancelled: false))
            .WithExpiresAt(DateTimeOffset.UtcNow.AddDays(30));

        if (_seedExpiredCoupon)
        {
            // Override with a past expiry.
            builder.WithExpiresAt(DateTimeOffset.UtcNow.AddMinutes(-1));
        }

        var coupon = builder.Build();
        CouponId = coupon.Id;

        if (_seedRedeemedCoupon)
        {
            coupon.Redeem();
        }

        if (_seedRevokedCoupon)
        {
            coupon.Revoke();
        }

        await environment.Database.SeedAsync(dbContext =>
        {
            dbContext.Coupons.Add(coupon);
        });
    }
}
