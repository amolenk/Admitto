using Amolenk.Admitto.Module.Registrations.Domain.Tests.Builders;
using Amolenk.Admitto.Module.Registrations.Domain.ValueObjects;
using Amolenk.Admitto.Module.Registrations.Tests.Application.Infrastructure.Hosting;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Module.Registrations.Tests.Application.UseCases.CouponManagement.GetCouponDetails;

internal sealed class GetCouponDetailsFixture
{
    private bool _seedCoupon;

    public TicketedEventId EventId { get; } = TicketedEventId.New();
    public string TicketTypeSlug { get; } = "general-admission";
    public CouponId CouponId { get; private set; } = CouponId.New();

    private GetCouponDetailsFixture()
    {
    }

    public static GetCouponDetailsFixture WithCoupon() => new()
    {
        _seedCoupon = true
    };

    public static GetCouponDetailsFixture NoCoupon() => new();

    public async ValueTask SetupAsync(IntegrationTestEnvironment environment)
    {
        if (!_seedCoupon)
        {
            return;
        }

        var coupon = new CouponBuilder()
            .WithEventId(EventId)
            .WithEmail(EmailAddress.From("speaker@example.com"))
            .WithRequestedTicketTypeSlugs(TicketTypeSlug)
            .WithAvailableTicketTypes(new TicketTypeInfo(TicketTypeSlug, IsCancelled: false))
            .WithExpiresAt(DateTimeOffset.UtcNow.AddDays(30))
            .WithBypassRegistrationWindow()
            .Build();

        CouponId = coupon.Id;

        await environment.Database.SeedAsync(dbContext =>
        {
            dbContext.Coupons.Add(coupon);
        });
    }
}
