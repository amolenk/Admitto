using Amolenk.Admitto.Core.Registrations.Domain.ValueObjects;
using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;
using Amolenk.Admitto.Testing.Builders.Registrations.Domain;

namespace Amolenk.Admitto.Core.IntegrationTests.Registrations.Application.UseCases.CouponManagement.ListCoupons;

internal sealed class ListCouponsFixture
{
    private bool _seedCoupons;
    private bool _seedMixedSources;

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

    public static ListCouponsFixture WithMixedSources() => new()
    {
        _seedMixedSources = true
    };

    public async ValueTask SetupAsync(IntegrationTestEnvironment environment)
    {
        if (_seedCoupons)
        {
            await SeedCouponsAsync(environment);
        }
        else if (_seedMixedSources)
        {
            await SeedMixedSourcesAsync(environment);
        }
    }

    private async ValueTask SeedCouponsAsync(IntegrationTestEnvironment environment)
    {
        // Seed an active coupon.
        var activeCoupon = new CouponBuilder()
            .WithEventId(EventId)
            .WithEmail(EmailAddress.From("active@example.com"))
            .WithRequestedTicketTypeIds(TicketTypeId)
            .WithAvailableTicketTypes(new TicketTypeInfo(TicketTypeId))
            .WithExpiresAt(DateTimeOffset.UtcNow.AddDays(30))
            .Build();

        // Seed a revoked coupon.
        var revokedCoupon = new CouponBuilder()
            .WithEventId(EventId)
            .WithEmail(EmailAddress.From("revoked@example.com"))
            .WithRequestedTicketTypeIds(TicketTypeId)
            .WithAvailableTicketTypes(new TicketTypeInfo(TicketTypeId))
            .WithExpiresAt(DateTimeOffset.UtcNow.AddDays(30))
            .Build();
        revokedCoupon.Revoke();

        await environment.RegistrationsDatabase.SeedAsync(dbContext =>
        {
            dbContext.Coupons.Add(activeCoupon);
            dbContext.Coupons.Add(revokedCoupon);
        });
    }

    private async ValueTask SeedMixedSourcesAsync(IntegrationTestEnvironment environment)
    {
        var organiserCoupon = new CouponBuilder()
            .WithEventId(EventId)
            .WithEmail(EmailAddress.From("organiser@example.com"))
            .WithRequestedTicketTypeIds(TicketTypeId)
            .WithAvailableTicketTypes(new TicketTypeInfo(TicketTypeId))
            .WithExpiresAt(DateTimeOffset.UtcNow.AddDays(30))
            .WithSource(CouponSource.Organiser)
            .Build();

        var waitlistCoupon = new CouponBuilder()
            .WithEventId(EventId)
            .WithEmail(EmailAddress.From("waitlist@example.com"))
            .WithRequestedTicketTypeIds(TicketTypeId)
            .WithAvailableTicketTypes(new TicketTypeInfo(TicketTypeId))
            .WithExpiresAt(DateTimeOffset.UtcNow.AddDays(30))
            .WithSource(CouponSource.Waitlist)
            .Build();

        await environment.RegistrationsDatabase.SeedAsync(dbContext =>
        {
            dbContext.Coupons.Add(organiserCoupon);
            dbContext.Coupons.Add(waitlistCoupon);
        });
    }
}
