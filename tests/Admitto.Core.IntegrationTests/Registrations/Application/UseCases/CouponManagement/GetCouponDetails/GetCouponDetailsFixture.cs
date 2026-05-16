using Amolenk.Admitto.Core.Registrations.Domain.ValueObjects;
using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;
using Amolenk.Admitto.Testing.Builders.Registrations.Domain;

namespace Amolenk.Admitto.Core.IntegrationTests.Registrations.Application.UseCases.CouponManagement.GetCouponDetails;

internal sealed class GetCouponDetailsFixture
{
    private bool _seedCoupon;

    public TicketedEventId EventId { get; } = TicketedEventId.New();
    public TicketTypeId TicketTypeId { get; } = TicketTypeId.From(new Guid("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"));
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
            .WithRequestedTicketTypeIds(TicketTypeId)
            .WithAvailableTicketTypes(new TicketTypeInfo(TicketTypeId))
            .WithExpiresAt(DateTimeOffset.UtcNow.AddDays(30))
            .WithBypassRegistrationWindow()
            .Build();

        CouponId = coupon.Id;

        await environment.RegistrationsDatabase.SeedAsync(dbContext =>
        {
            dbContext.Coupons.Add(coupon);
        });
    }
}
