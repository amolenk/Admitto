using Amolenk.Admitto.Core.Registrations.Domain.ValueObjects;
using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;
using Amolenk.Admitto.Testing.Builders.Registrations.Domain;

namespace Amolenk.Admitto.Core.IntegrationTests.Registrations.Application.UseCases.Coupons.RevokeCoupon;

internal sealed class RevokeCouponFixture
{
    private bool _seedActiveCoupon;
    private bool _seedRedeemedCoupon;
    private bool _seedRevokedCoupon;
    private bool _seedExpiredCoupon;

    public TicketedEventId EventId { get; } = TicketedEventId.New();
    public TeamId TeamId { get; } = TeamId.New();
    public TicketTypeId TicketTypeId { get; } = TicketTypeId.New();
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
            .WithTeamId(TeamId)
            .WithRequestedTicketTypeIds(TicketTypeId)
            .WithAvailableTicketTypes(new TicketTypeInfo(TicketTypeId))
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
            coupon.Redeem(coupon.Email, coupon.AllowedTicketTypeIds, DateTimeOffset.UtcNow);
        }

        if (_seedRevokedCoupon)
        {
            coupon.Revoke();
        }

        await environment.RegistrationsDatabase.SeedAsync(dbContext =>
        {
            dbContext.Coupons.Add(coupon);
        });
    }
}
