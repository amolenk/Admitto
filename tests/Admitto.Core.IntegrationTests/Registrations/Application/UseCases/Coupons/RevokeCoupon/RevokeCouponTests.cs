using Amolenk.Admitto.Core.Registrations.Application.UseCases.Coupons.RevokeCoupon;
using Amolenk.Admitto.Core.Registrations.Domain.Entities;
using Amolenk.Admitto.Core.Shared.Kernel.ErrorHandling;
using Amolenk.Admitto.Testing.Infrastructure.Assertions;
using Microsoft.EntityFrameworkCore;

namespace Amolenk.Admitto.Core.IntegrationTests.Registrations.Application.UseCases.Coupons.RevokeCoupon;

[TestClass]
public sealed class RevokeCouponTests(TestContext testContext) : AspireIntegrationTestBase
{
    // Given an active coupon
    // When a RevokeCoupon command is handled
    // Then the coupon is marked as revoked
    [TestMethod]
    public async ValueTask RevokeCoupon_ActiveCoupon_SetsRevokedAt()
    {
        // Arrange
        var fixture = RevokeCouponFixture.ActiveCoupon();
        await fixture.SetupAsync(Environment);

        var command = new RevokeCouponCommand(fixture.EventId.Value, fixture.TeamId.Value, fixture.CouponId.Value);
        var sut = NewRevokeCouponHandler();

        // Act
        await sut.HandleAsync(command, testContext.CancellationToken);

        // Assert
        await Environment.RegistrationsDatabase.AssertAsync(async dbContext =>
        {
            var coupon = await dbContext.Coupons.SingleOrDefaultAsync(testContext.CancellationToken);
            coupon.ShouldNotBeNull().RevokedAt.ShouldNotBeNull();
        });
    }

    // Given a coupon that has already expired
    // When a RevokeCoupon command is handled
    // Then the coupon is marked as revoked
    [TestMethod]
    public async ValueTask RevokeCoupon_ExpiredCoupon_SetsRevokedAt()
    {
        // Arrange
        var fixture = RevokeCouponFixture.ExpiredCoupon();
        await fixture.SetupAsync(Environment);

        var command = new RevokeCouponCommand(fixture.EventId.Value, fixture.TeamId.Value, fixture.CouponId.Value);
        var sut = NewRevokeCouponHandler();

        // Act
        await sut.HandleAsync(command, testContext.CancellationToken);

        // Assert
        await Environment.RegistrationsDatabase.AssertAsync(async dbContext =>
        {
            var coupon = await dbContext.Coupons.SingleOrDefaultAsync(testContext.CancellationToken);
            coupon.ShouldNotBeNull().RevokedAt.ShouldNotBeNull();
        });
    }

    // Given a coupon that has already been redeemed
    // When a RevokeCoupon command is handled
    // Then a coupon-already-redeemed error is thrown
    [TestMethod]
    public async ValueTask RevokeCoupon_RedeemedCoupon_ThrowsCouponAlreadyRedeemedError()
    {
        // Arrange
        var fixture = RevokeCouponFixture.RedeemedCoupon();
        await fixture.SetupAsync(Environment);

        var command = new RevokeCouponCommand(fixture.EventId.Value, fixture.TeamId.Value, fixture.CouponId.Value);
        var sut = NewRevokeCouponHandler();

        // Act
        var result = await ErrorResult.CaptureAsync(
            async () => { await sut.HandleAsync(command, testContext.CancellationToken); });

        // Assert
        result.Error.ShouldMatch(Coupon.Errors.CouponAlreadyRedeemed);
    }

    // NFR-003: Idempotent revocation
    // Given a coupon that has already been revoked
    // When a RevokeCoupon command is handled again
    // Then it does not throw and the coupon remains revoked
    [TestMethod]
    public async ValueTask RevokeCoupon_AlreadyRevoked_IsIdempotent()
    {
        // Arrange
        var fixture = RevokeCouponFixture.AlreadyRevokedCoupon();
        await fixture.SetupAsync(Environment);

        var command = new RevokeCouponCommand(fixture.EventId.Value, fixture.TeamId.Value, fixture.CouponId.Value);
        var sut = NewRevokeCouponHandler();

        // Act — should not throw
        await sut.HandleAsync(command, testContext.CancellationToken);

        // Assert
        await Environment.RegistrationsDatabase.AssertAsync(async dbContext =>
        {
            var coupon = await dbContext.Coupons.SingleOrDefaultAsync(testContext.CancellationToken);
            coupon.ShouldNotBeNull().RevokedAt.ShouldNotBeNull();
        });
    }

    // Given no coupon exists with the given id
    // When a RevokeCoupon command is handled
    // Then a not-found error is thrown
    [TestMethod]
    public async ValueTask RevokeCoupon_NonExistentCoupon_ThrowsNotFoundError()
    {
        // Arrange
        var fixture = RevokeCouponFixture.NoCoupon();
        await fixture.SetupAsync(Environment);

        var command = new RevokeCouponCommand(fixture.EventId.Value, fixture.TeamId.Value, fixture.CouponId.Value);
        var sut = NewRevokeCouponHandler();

        // Act
        var result = await ErrorResult.CaptureAsync(
            async () => { await sut.HandleAsync(command, testContext.CancellationToken); });

        // Assert
        result.Error.ShouldMatch(NotFoundError.Create<Coupon>());
    }

    private static RevokeCouponHandler NewRevokeCouponHandler() =>
        new(Environment.RegistrationsDatabase.Context);
}
