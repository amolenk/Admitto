using Amolenk.Admitto.Core.Registrations.Domain.Entities;
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

        var coupon = (_seedActiveCoupon || _seedRedeemedCoupon || _seedRevokedCoupon || _seedExpiredCoupon)
            ? builder.Build()
            : null;
        CouponId = coupon?.Id ?? CouponId;

        if (_seedRedeemedCoupon && coupon is not null)
        {
            coupon.Redeem(coupon.Email, coupon.AllowedTicketTypeIds, DateTimeOffset.UtcNow);
        }

        if (_seedRevokedCoupon && coupon is not null)
        {
            coupon.Revoke();
        }

        await environment.RegistrationsDatabase.SeedAsync(dbContext =>
        {
            var catalog = TicketCatalog.Create(EventId, TeamId);
            catalog.AddTicketType(TicketTypeId, TicketTypeName.From("General Admission"), [], 100);
            dbContext.TicketCatalogs.Add(catalog);

            if (coupon is not null)
                dbContext.Coupons.Add(coupon);
        });
    }
}
