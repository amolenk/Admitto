using Amolenk.Admitto.Core.Registrations.Domain.DomainEvents;
using Amolenk.Admitto.Core.Registrations.Domain.Entities;
using Amolenk.Admitto.Testing.Builders.Registrations.Domain;
using Amolenk.Admitto.Core.Registrations.Domain.ValueObjects;
using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;
using Amolenk.Admitto.Testing.Infrastructure.Assertions;
using Shouldly;

namespace Amolenk.Admitto.Core.Registrations.Domain.Tests.Entities;

[TestClass]
public sealed class CouponTests
{
    [TestMethod]
    public void Create_ValidInput_CreatesCouponAndRaisesDomainEvent()
    {
        // Arrange
        var ticketTypeId = TicketTypeId.New();
        var email = EmailAddress.From("speaker@example.com");

        // Act
        var sut = new CouponBuilder()
            .WithEmail(email)
            .WithRequestedTicketTypeIds(ticketTypeId)
            .WithAvailableTicketTypes(new TicketTypeInfo(ticketTypeId))
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

    [TestMethod]
    public void Create_BypassRegistrationWindow_SetsBypassFlag()
    {
        // Act
        var sut = new CouponBuilder()
            .WithBypassRegistrationWindow()
            .Build();

        // Assert
        sut.BypassRegistrationWindow.ShouldBeTrue();
    }

    [TestMethod]
    public void Create_UnknownTicketType_ThrowsUnknownTicketTypesError()
    {
        // Arrange
        var unknownId = TicketTypeId.New();
        var knownId = TicketTypeId.New();

        // Act
        var result = ErrorResult.Capture(() =>
            new CouponBuilder()
                .WithRequestedTicketTypeIds(unknownId)
                .WithAvailableTicketTypes(new TicketTypeInfo(knownId))
                .Build());

        // Assert
        result.Error.ShouldMatch(Coupon.Errors.UnknownTicketTypes(new List<Guid> { unknownId.Value }));
    }

    [TestMethod]
    public void Create_ExpiryInThePast_ThrowsExpiryMustBeInFutureError()
    {
        // Act
        var result = ErrorResult.Capture(() =>
            new CouponBuilder()
                .WithExpiresAt(new DateTimeOffset(2020, 1, 1, 0, 0, 0, TimeSpan.Zero))
                .Build());

        // Assert
        result.Error.ShouldMatch(Coupon.Errors.ExpiryMustBeInFuture);
    }

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

    [TestMethod]
    public void GetStatus_VariousCouponStates_ReturnsCorrectStatus()
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

    [TestMethod]
    public void Create_ValidInput_AllPropertiesAccessible()
    {
        // Arrange
        var id1 = TicketTypeId.New();
        var id2 = TicketTypeId.New();
        var email = EmailAddress.From("speaker@example.com");
        var expiresAt = new DateTimeOffset(2025, 12, 31, 0, 0, 0, TimeSpan.Zero);

        // Act
        var sut = new CouponBuilder()
            .WithEmail(email)
            .WithRequestedTicketTypeIds(id1, id2)
            .WithAvailableTicketTypes(
                new TicketTypeInfo(id1),
                new TicketTypeInfo(id2))
            .WithExpiresAt(expiresAt)
            .WithBypassRegistrationWindow()
            .Build();

        // Assert
        sut.ShouldSatisfyAllConditions(
            () => sut.Id.Value.ShouldNotBe(Guid.Empty),
            () => sut.Code.Value.ShouldNotBe(Guid.Empty),
            () => sut.Email.ShouldBe(email),
            () => sut.AllowedTicketTypeIds.Count.ShouldBe(2),
            () => sut.AllowedTicketTypeIds.ShouldContain(id1),
            () => sut.AllowedTicketTypeIds.ShouldContain(id2),
            () => sut.ExpiresAt.ShouldBe(expiresAt),
            () => sut.BypassRegistrationWindow.ShouldBeTrue(),
            () => sut.RedeemedAt.ShouldBeNull(),
            () => sut.RevokedAt.ShouldBeNull());
    }

    [TestMethod]
    public void Revoke_ActiveCoupon_SetsRevokedAt()
    {
        // Arrange
        var sut = new CouponBuilder().Build();

        // Act
        sut.Revoke();

        // Assert
        sut.RevokedAt.ShouldNotBeNull();
        sut.GetStatus(CouponBuilder.DefaultNow).ShouldBe(CouponStatus.Revoked);
    }

    [TestMethod]
    public void Revoke_ExpiredCoupon_SetsRevokedAt()
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

    [TestMethod]
    public void Revoke_RedeemedCoupon_ThrowsCouponAlreadyRedeemedError()
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

    [TestMethod]
    public void Revoke_AlreadyRevokedCoupon_IsIdempotent()
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
