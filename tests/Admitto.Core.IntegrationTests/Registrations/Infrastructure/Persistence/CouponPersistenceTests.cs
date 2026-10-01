using Amolenk.Admitto.Core.Registrations.Domain.ValueObjects;
using Amolenk.Admitto.Testing.Builders.Registrations.Domain;
using Microsoft.EntityFrameworkCore;

namespace Amolenk.Admitto.Core.IntegrationTests.Registrations.Infrastructure.Persistence;

[TestClass]
public sealed class CouponPersistenceTests(TestContext testContext) : AspireIntegrationTestBase
{
    // Given an organiser coupon and waitlist coupons of each origin
    // When they are saved and loaded again
    // Then each keeps its waitlist origin, and with it the pool its redemption claims from
    [TestMethod]
    [DataRow(null, ClaimMode.Admin)]
    [DataRow(WaitlistCouponOrigin.Automatic, ClaimMode.Public)]
    [DataRow(WaitlistCouponOrigin.Manual, ClaimMode.Admin)]
    public async ValueTask Load_WaitlistOrigin_RoundTrips(WaitlistCouponOrigin? origin, ClaimMode expectedClaimMode)
    {
        // Arrange
        var builder = new CouponBuilder().WithExpiresAt(DateTimeOffset.UtcNow.AddDays(1)).WithNow(DateTimeOffset.UtcNow);
        var coupon = (origin is { } o ? builder.WithWaitlistOrigin(o) : builder).Build();

        // Act
        await Environment.RegistrationsDatabase.SeedAsync(
            dbContext => dbContext.Coupons.Add(coupon), testContext.CancellationToken);

        // Assert
        await Environment.RegistrationsDatabase.AssertAsync(async ctx =>
        {
            var loaded = await ctx.Coupons.SingleAsync(c => c.Id == coupon.Id, testContext.CancellationToken);
            loaded.WaitlistOrigin.ShouldBe(origin);
            loaded.RedemptionClaimMode.ShouldBe(expectedClaimMode);
        });
    }
}
