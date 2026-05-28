using Amolenk.Admitto.Core.Registrations.Domain.Entities;
using Amolenk.Admitto.Core.Registrations.Domain.ValueObjects;
using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;
using Amolenk.Admitto.Testing.Builders.Registrations.Domain;

namespace Amolenk.Admitto.Core.IntegrationTests.Registrations.Application.UseCases.Coupons.GetPublicCouponDetails;

internal sealed class GetPublicCouponDetailsFixture
{
    private bool _seedCoupon;
    private bool _redeemedCoupon;

    public TicketedEventId EventId { get; } = TicketedEventId.New();
    public TicketTypeId TicketTypeId { get; } = TicketTypeId.From(new Guid("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"));
    public string TicketTypeName { get; } = "General Admission";
    public CouponCode CouponCode { get; private set; } = CouponCode.New();
    private GetPublicCouponDetailsFixture()
    {
    }

    public static GetPublicCouponDetailsFixture WithActiveCoupon() => new()
    {
        _seedCoupon = true
    };

    public static GetPublicCouponDetailsFixture WithRedeemedCoupon() => new()
    {
        _seedCoupon = true,
        _redeemedCoupon = true
    };

    public static GetPublicCouponDetailsFixture NoCoupon() => new();

    public async ValueTask SetupAsync(IntegrationTestEnvironment environment)
    {
        var catalog = TicketCatalog.Create(EventId, TeamId.New());
        catalog.AddTicketType(TicketTypeId, Amolenk.Admitto.Core.Registrations.Domain.ValueObjects.TicketTypeName.From(this.TicketTypeName), [], 100);

        if (_seedCoupon)
        {
            var coupon = new CouponBuilder()
                .WithEventId(EventId)
                .WithRequestedTicketTypeIds(TicketTypeId)
                .WithAvailableTicketTypes(new TicketTypeInfo(TicketTypeId))
                .WithExpiresAt(DateTimeOffset.UtcNow.AddDays(30))
                .Build();

            if (_redeemedCoupon)
            {
                coupon.Redeem(coupon.Email, coupon.AllowedTicketTypeIds, DateTimeOffset.UtcNow);
            }

            CouponCode = coupon.Code;

            await environment.RegistrationsDatabase.SeedAsync(dbContext =>
            {
                dbContext.TicketCatalogs.Add(catalog);
                dbContext.Coupons.Add(coupon);
            });
        }
        else
        {
            await environment.RegistrationsDatabase.SeedAsync(dbContext =>
            {
                dbContext.TicketCatalogs.Add(catalog);
            });
        }
    }
}
