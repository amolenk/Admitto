using Amolenk.Admitto.Module.Registrations.Application.UseCases.CouponManagement.RevokeCoupon;
using Amolenk.Admitto.Module.Registrations.Domain.Entities;
using Amolenk.Admitto.Module.Registrations.Domain.ValueObjects;
using Amolenk.Admitto.Module.Registrations.Tests.Application.Aspire;
using Amolenk.Admitto.Module.Shared.Kernel.ErrorHandling;
using Amolenk.Admitto.Testing.Infrastructure.Assertions;
using Microsoft.EntityFrameworkCore;

namespace Amolenk.Admitto.Module.Registrations.Tests.Application.UseCases.CouponManagement.RevokeCoupon;

[TestClass]
public sealed class RevokeCouponTests(TestContext testContext) : AspireIntegrationTestBase
{
    // SC-010: Successful revocation
    [TestMethod]
    public async ValueTask SC010_RevokeCoupon_ActiveCoupon_SetsRevokedAt()
    {
        // Arrange
        var fixture = RevokeCouponFixture.ActiveCoupon();
        await fixture.SetupAsync(Environment);

        var command = new RevokeCouponCommand(fixture.EventId, fixture.CouponId);
        var sut = NewRevokeCouponHandler();

        // Act
        await sut.HandleAsync(command, testContext.CancellationToken);

        // Assert
        await Environment.Database.AssertAsync(async dbContext =>
        {
            var coupon = await dbContext.Coupons.SingleOrDefaultAsync(testContext.CancellationToken);
            coupon.ShouldNotBeNull().RevokedAt.ShouldNotBeNull();
        });
    }

    // SC-011: Revoke already-expired coupon — succeeds
    [TestMethod]
    public async ValueTask SC011_RevokeCoupon_ExpiredCoupon_SetsRevokedAt()
    {
        // Arrange
        var fixture = RevokeCouponFixture.ExpiredCoupon();
        await fixture.SetupAsync(Environment);

        var command = new RevokeCouponCommand(fixture.EventId, fixture.CouponId);
        var sut = NewRevokeCouponHandler();

        // Act
        await sut.HandleAsync(command, testContext.CancellationToken);

        // Assert
        await Environment.Database.AssertAsync(async dbContext =>
        {
            var coupon = await dbContext.Coupons.SingleOrDefaultAsync(testContext.CancellationToken);
            coupon.ShouldNotBeNull().RevokedAt.ShouldNotBeNull();
        });
    }

    // SC-012: Rejected — revoke redeemed coupon
    [TestMethod]
    public async ValueTask SC012_RevokeCoupon_RedeemedCoupon_ThrowsCouponAlreadyRedeemedError()
    {
        // Arrange
        var fixture = RevokeCouponFixture.RedeemedCoupon();
        await fixture.SetupAsync(Environment);

        var command = new RevokeCouponCommand(fixture.EventId, fixture.CouponId);
        var sut = NewRevokeCouponHandler();

        // Act
        var result = await ErrorResult.CaptureAsync(
            async () => { await sut.HandleAsync(command, testContext.CancellationToken); });

        // Assert
        result.Error.ShouldMatch(Coupon.Errors.CouponAlreadyRedeemed);
    }

    // NFR-003: Idempotent revocation
    [TestMethod]
    public async ValueTask RevokeCoupon_AlreadyRevoked_IsIdempotent()
    {
        // Arrange
        var fixture = RevokeCouponFixture.AlreadyRevokedCoupon();
        await fixture.SetupAsync(Environment);

        var command = new RevokeCouponCommand(fixture.EventId, fixture.CouponId);
        var sut = NewRevokeCouponHandler();

        // Act — should not throw
        await sut.HandleAsync(command, testContext.CancellationToken);

        // Assert
        await Environment.Database.AssertAsync(async dbContext =>
        {
            var coupon = await dbContext.Coupons.SingleOrDefaultAsync(testContext.CancellationToken);
            coupon.ShouldNotBeNull().RevokedAt.ShouldNotBeNull();
        });
    }

    [TestMethod]
    public async ValueTask RevokeCoupon_NonExistentCoupon_ThrowsNotFoundError()
    {
        // Arrange
        var fixture = RevokeCouponFixture.NoCoupon();
        await fixture.SetupAsync(Environment);

        var command = new RevokeCouponCommand(fixture.EventId, fixture.CouponId);
        var sut = NewRevokeCouponHandler();

        // Act
        var result = await ErrorResult.CaptureAsync(
            async () => { await sut.HandleAsync(command, testContext.CancellationToken); });

        // Assert
        result.Error.ShouldMatch(NotFoundError.Create<Coupon>(fixture.CouponId.Value));
    }

    private static RevokeCouponHandler NewRevokeCouponHandler() =>
        new(Environment.Database.Context);
}
