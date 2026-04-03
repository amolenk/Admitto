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
    [TestMethod]
    public void Create_ValidInput_CreatesCouponAndRaisesDomainEvent()
    {
        // Arrange
        var slug = "general-admission";
        var email = EmailAddress.From("speaker@example.com");

        // Act
        var sut = new CouponBuilder()
            .WithEmail(email)
            .WithRequestedTicketTypeSlugs(slug)
            .WithAvailableTicketTypes(new TicketTypeInfo(slug, IsCancelled: false))
            .Build();

        // Assert
        sut.EventId.ShouldBe(CouponBuilder.DefaultEventId);
        sut.Email.ShouldBe(email);
        sut.AllowedTicketTypeSlugs.ShouldContain(slug);
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
        var unknownSlug = "unknown-type";
        var knownSlug = "general-admission";

        // Act
        var result = ErrorResult.Capture(() =>
            new CouponBuilder()
                .WithRequestedTicketTypeSlugs(unknownSlug)
                .WithAvailableTicketTypes(new TicketTypeInfo(knownSlug, IsCancelled: false))
                .Build());

        // Assert
        result.Error.ShouldMatch(Coupon.Errors.UnknownTicketTypes(new List<string> { unknownSlug }));
    }

    [TestMethod]
    public void Create_CancelledTicketType_ThrowsCancelledTicketTypesError()
    {
        // Arrange
        var cancelledSlug = "vip-pass";

        // Act
        var result = ErrorResult.Capture(() =>
            new CouponBuilder()
                .WithRequestedTicketTypeSlugs(cancelledSlug)
                .WithAvailableTicketTypes(new TicketTypeInfo(cancelledSlug, IsCancelled: true))
                .Build());

        // Assert
        result.Error.ShouldMatch(Coupon.Errors.CancelledTicketTypes(new List<string> { cancelledSlug }));
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
                .WithRequestedTicketTypeSlugs()
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
        var slug1 = "general-admission";
        var slug2 = "vip-pass";
        var email = EmailAddress.From("speaker@example.com");
        var expiresAt = new DateTimeOffset(2025, 12, 31, 0, 0, 0, TimeSpan.Zero);

        // Act
        var sut = new CouponBuilder()
            .WithEmail(email)
            .WithRequestedTicketTypeSlugs(slug1, slug2)
            .WithAvailableTicketTypes(
                new TicketTypeInfo(slug1, IsCancelled: false),
                new TicketTypeInfo(slug2, IsCancelled: false))
            .WithExpiresAt(expiresAt)
            .WithBypassRegistrationWindow()
            .Build();

        // Assert
        sut.ShouldSatisfyAllConditions(
            () => sut.Id.Value.ShouldNotBe(Guid.Empty),
            () => sut.Code.Value.ShouldNotBe(Guid.Empty),
            () => sut.Email.ShouldBe(email),
            () => sut.AllowedTicketTypeSlugs.Count.ShouldBe(2),
            () => sut.AllowedTicketTypeSlugs.ShouldContain(slug1),
            () => sut.AllowedTicketTypeSlugs.ShouldContain(slug2),
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

