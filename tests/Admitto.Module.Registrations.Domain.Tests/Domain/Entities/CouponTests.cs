using Amolenk.Admitto.Module.Registrations.Domain.DomainEvents;
using Amolenk.Admitto.Module.Registrations.Domain.Entities;
using Amolenk.Admitto.Module.Registrations.Domain.Tests.Builders;
using Amolenk.Admitto.Module.Registrations.Domain.ValueObjects;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;
using Amolenk.Admitto.Testing.Infrastructure.Assertions;
using Shouldly;

namespace Amolenk.Admitto.Module.Registrations.Domain.Tests.Entities;

[TestClass]
public sealed class CouponTests
{
    // SC-001: Successful coupon creation
    [TestMethod]
    public void SC001_Create_ValidInput_CreatesCouponAndRaisesDomainEvent()
    {
        // Arrange
        var ticketTypeId = TicketTypeId.New();
        var email = EmailAddress.From("speaker@example.com");

        // Act
        var sut = new CouponBuilder()
            .WithEmail(email)
            .WithRequestedTicketTypeIds(ticketTypeId)
            .WithAvailableTicketTypes(new TicketTypeInfo(ticketTypeId, IsCancelled: false))
            .Build();

        // Assert
        sut.EventId.ShouldBe(CouponBuilder.DefaultEventId);
        sut.Email.ShouldBe(email);
        sut.AllowedTicketTypeIds.ShouldContain(ticketTypeId);
        sut.ExpiresAt.ShouldBe(CouponBuilder.DefaultExpiresAt);
        sut.BypassRegistrationWindow.ShouldBeFalse();
        sut.GetStatus(CouponBuilder.DefaultNow).ShouldBe(CouponStatus.Active);

        sut.GetDomainEvents()
            .ShouldHaveSingleItem()
            .ShouldBeAssignableTo<CouponCreatedDomainEvent>()
            .ShouldSatisfyAllConditions(
                e => e.CouponId.ShouldBe(sut.Id),
                e => e.TicketedEventId.ShouldBe(sut.EventId),
                e => e.Email.ShouldBe(email));
    }

    // SC-002: Coupon with registration window bypass
    [TestMethod]
    public void SC002_Create_BypassRegistrationWindow_SetsBypassFlag()
    {
        // Act
        var sut = new CouponBuilder()
            .WithBypassRegistrationWindow()
            .Build();

        // Assert
        sut.BypassRegistrationWindow.ShouldBeTrue();
    }

    // SC-003: Rejected — ticket type does not exist
    [TestMethod]
    public void SC003_Create_UnknownTicketType_ThrowsUnknownTicketTypesError()
    {
        // Arrange
        var unknownTicketTypeId = TicketTypeId.New();
        var knownTicketTypeId = TicketTypeId.New();

        // Act
        var result = ErrorResult.Capture(() =>
            new CouponBuilder()
                .WithRequestedTicketTypeIds(unknownTicketTypeId)
                .WithAvailableTicketTypes(new TicketTypeInfo(knownTicketTypeId, IsCancelled: false))
                .Build());

        // Assert
        result.Error.ShouldMatch(Coupon.Errors.UnknownTicketTypes([unknownTicketTypeId]));
    }

    // SC-004: Rejected — ticket type is cancelled
    [TestMethod]
    public void SC004_Create_CancelledTicketType_ThrowsCancelledTicketTypesError()
    {
        // Arrange
        var cancelledTicketTypeId = TicketTypeId.New();

        // Act
        var result = ErrorResult.Capture(() =>
            new CouponBuilder()
                .WithRequestedTicketTypeIds(cancelledTicketTypeId)
                .WithAvailableTicketTypes(new TicketTypeInfo(cancelledTicketTypeId, IsCancelled: true))
                .Build());

        // Assert
        result.Error.ShouldMatch(Coupon.Errors.CancelledTicketTypes([cancelledTicketTypeId]));
    }

    // SC-005: Rejected — expiry in the past
    [TestMethod]
    public void SC005_Create_ExpiryInThePast_ThrowsExpiryMustBeInFutureError()
    {
        // Act
        var result = ErrorResult.Capture(() =>
            new CouponBuilder()
                .WithExpiresAt(new DateTimeOffset(2020, 1, 1, 0, 0, 0, TimeSpan.Zero))
                .Build());

        // Assert
        result.Error.ShouldMatch(Coupon.Errors.ExpiryMustBeInFuture);
    }

    // SC-006: Rejected — cancelled event (handler-level concern, tested via CreateCouponHandlerTests)

    // Supplementary: No ticket types specified (domain validation)
    [TestMethod]
    public void Create_NoTicketTypes_ThrowsNoTicketTypesError()
    {
        // Act
        var result = ErrorResult.Capture(() =>
            new CouponBuilder()
                .WithRequestedTicketTypeIds()
                .Build());

        // Assert
        result.Error.ShouldMatch(Coupon.Errors.NoTicketTypes);
    }

    // SC-007: List coupons — verifies status computation for different coupon states
    [TestMethod]
    public void SC007_GetStatus_VariousCouponStates_ReturnsCorrectStatus()
    {
        // Arrange
        var now = CouponBuilder.DefaultNow;

        var activeCoupon = new CouponBuilder().Build();

        var revokedCoupon = new CouponBuilder().Build();
        revokedCoupon.Revoke();

        var expiredCoupon = new CouponBuilder()
            .WithExpiresAt(now.AddHours(1))
            .Build();

        // Assert
        activeCoupon.GetStatus(now).ShouldBe(CouponStatus.Active);
        revokedCoupon.GetStatus(now).ShouldBe(CouponStatus.Revoked);
        expiredCoupon.GetStatus(now.AddHours(2)).ShouldBe(CouponStatus.Expired);
    }

    // SC-008: Empty coupon list — no domain test needed (query-level concern)

    // SC-009: View coupon details — verifies all properties are accessible
    [TestMethod]
    public void SC009_Create_ValidInput_AllPropertiesAccessible()
    {
        // Arrange
        var ticketTypeId1 = TicketTypeId.New();
        var ticketTypeId2 = TicketTypeId.New();
        var email = EmailAddress.From("speaker@example.com");
        var expiresAt = new DateTimeOffset(2025, 12, 31, 0, 0, 0, TimeSpan.Zero);

        // Act
        var sut = new CouponBuilder()
            .WithEmail(email)
            .WithRequestedTicketTypeIds(ticketTypeId1, ticketTypeId2)
            .WithAvailableTicketTypes(
                new TicketTypeInfo(ticketTypeId1, IsCancelled: false),
                new TicketTypeInfo(ticketTypeId2, IsCancelled: false))
            .WithExpiresAt(expiresAt)
            .WithBypassRegistrationWindow()
            .Build();

        // Assert
        sut.ShouldSatisfyAllConditions(
            () => sut.Id.Value.ShouldNotBe(Guid.Empty),
            () => sut.Code.Value.ShouldNotBe(Guid.Empty),
            () => sut.Email.ShouldBe(email),
            () => sut.AllowedTicketTypeIds.Count.ShouldBe(2),
            () => sut.AllowedTicketTypeIds.ShouldContain(ticketTypeId1),
            () => sut.AllowedTicketTypeIds.ShouldContain(ticketTypeId2),
            () => sut.ExpiresAt.ShouldBe(expiresAt),
            () => sut.BypassRegistrationWindow.ShouldBeTrue(),
            () => sut.RedeemedAt.ShouldBeNull(),
            () => sut.RevokedAt.ShouldBeNull());
    }

    // SC-010: Successful revocation
    [TestMethod]
    public void SC010_Revoke_ActiveCoupon_SetsRevokedAt()
    {
        // Arrange
        var sut = new CouponBuilder().Build();

        // Act
        sut.Revoke();

        // Assert
        sut.RevokedAt.ShouldNotBeNull();
        sut.GetStatus(CouponBuilder.DefaultNow).ShouldBe(CouponStatus.Revoked);
    }

    // SC-011: Revoke already-expired coupon — succeeds
    [TestMethod]
    public void SC011_Revoke_ExpiredCoupon_SetsRevokedAt()
    {
        // Arrange
        var now = CouponBuilder.DefaultNow;

        var sut = new CouponBuilder()
            .WithExpiresAt(now.AddHours(1))
            .Build();

        var afterExpiry = now.AddHours(2);
        sut.GetStatus(afterExpiry).ShouldBe(CouponStatus.Expired);

        // Act
        sut.Revoke();

        // Assert
        sut.RevokedAt.ShouldNotBeNull();
        sut.GetStatus(afterExpiry).ShouldBe(CouponStatus.Revoked);
    }

    // SC-012: Rejected — revoke redeemed coupon
    [TestMethod]
    public void SC012_Revoke_RedeemedCoupon_ThrowsCouponAlreadyRedeemedError()
    {
        // Arrange — we need a redeemed coupon. Since Redeem isn't implemented yet,
        // we simulate by setting RedeemedAt via reflection (this is a domain-level test concern).
        var sut = new CouponBuilder().Build();
        SetRedeemedAt(sut, DateTimeOffset.UtcNow);
        sut.GetStatus(CouponBuilder.DefaultNow).ShouldBe(CouponStatus.Redeemed);

        // Act
        var result = ErrorResult.Capture(() => sut.Revoke());

        // Assert
        result.Error.ShouldMatch(Coupon.Errors.CouponAlreadyRedeemed);
    }

    // SC-010 supplement: Revoking an already-revoked coupon is idempotent (NFR-003)
    [TestMethod]
    public void SC010_Revoke_AlreadyRevokedCoupon_IsIdempotent()
    {
        // Arrange
        var sut = new CouponBuilder().Build();
        sut.Revoke();
        var firstRevokedAt = sut.RevokedAt;

        // Act
        sut.Revoke();

        // Assert
        sut.RevokedAt.ShouldBe(firstRevokedAt);
    }

    private static void SetRedeemedAt(Coupon coupon, DateTimeOffset redeemedAt)
    {
        var property = typeof(Coupon).GetProperty(nameof(Coupon.RedeemedAt))!;
        property.SetValue(coupon, redeemedAt);
    }
}
