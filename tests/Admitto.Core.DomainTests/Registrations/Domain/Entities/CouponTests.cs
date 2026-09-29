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
    // Given a coupon requested for a known ticket type with an organiser source
    // When the coupon is created
    // Then a single CouponCreated domain event is raised carrying the coupon's identity, email, and code
    [TestMethod]
    public void Create_OrganiserSource_RaisesCouponCreatedDomainEvent()
    {
        // Arrange
        var ticketTypeId = TicketTypeId.New();
        var email = EmailAddress.From("speaker@example.com");

        // Act
        var sut = new CouponBuilder()
            .WithEmail(email)
            .WithRequestedTicketTypeIds(ticketTypeId)
            .WithAvailableTicketTypes(new TicketTypeInfo(ticketTypeId))
            .WithSource(CouponSource.Organiser)
            .Build();

        // Assert
        sut.GetDomainEvents()
            .ShouldHaveSingleItem()
            .ShouldBeAssignableTo<CouponCreatedDomainEvent>()
            .ShouldSatisfyAllConditions(
                e => e.CouponId.ShouldBe(sut.Id),
                e => e.TeamId.ShouldBe(sut.TeamId),
                e => e.TicketedEventId.ShouldBe(sut.EventId),
                e => e.Email.ShouldBe(email),
                e => e.Code.ShouldBe(sut.Code));
    }

    // Given a coupon created from the waitlist source
    // When the coupon is created
    // Then no domain event is raised
    [TestMethod]
    public void Create_WaitlistSource_DoesNotRaiseCouponCreatedDomainEvent()
    {
        // Act
        var sut = new CouponBuilder()
            .WithSource(CouponSource.Waitlist)
            .Build();

        // Assert
        sut.GetDomainEvents().ShouldBeEmpty();
    }

    // Given valid coupon input with a known ticket type
    // When the coupon is created
    // Then it is Active with the given properties and raises a single CouponCreated domain event
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
                e => e.TeamId.ShouldBe(sut.TeamId),
                e => e.TicketedEventId.ShouldBe(sut.EventId),
                e => e.Email.ShouldBe(email),
                e => e.Code.ShouldBe(sut.Code));
    }

    // When a coupon is created with the bypass-registration-window option
    // Then its bypass flag is set to true
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

    // Given a coupon requested for a ticket type that is not among the available ticket types
    // When the coupon is created
    // Then it returns an UnknownTicketTypes error listing the unknown id
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

    // Given a coupon created with an expiry date in the past
    // When the coupon is created
    // Then it returns an ExpiryMustBeInFuture error
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

    // Given a coupon created with no requested ticket types
    // When the coupon is created
    // Then it returns a NoTicketTypes error
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

    // Given coupons that are active, redeemed, or past their expiry date
    // When their status is queried at the relevant time
    // Then each returns the matching status (Active, Redeemed, or Expired)
    [TestMethod]
    public void GetStatus_VariousCouponStates_ReturnsCorrectStatus()
    {
        // Arrange
        var now = CouponBuilder.DefaultNow;

        var activeCoupon = new CouponBuilder().Build();

        var redeemedCoupon = new CouponBuilder()
            .WithExpiresAt(now.AddHours(1))
            .Build();
        SetRedeemedAt(redeemedCoupon, now);

        var expiredCoupon = new CouponBuilder()
            .WithExpiresAt(now.AddHours(1))
            .Build();

        // Assert — a redeemed coupon stays Redeemed after its expiry passes
        activeCoupon.GetStatus(now).ShouldBe(CouponStatus.Active);
        redeemedCoupon.GetStatus(now.AddHours(2)).ShouldBe(CouponStatus.Redeemed);
        expiredCoupon.GetStatus(now.AddHours(2)).ShouldBe(CouponStatus.Expired);
    }

    // Given valid coupon input covering all optional properties
    // When the coupon is created
    // Then all of its properties are populated and accessible as expected
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
            () => sut.RedeemedAt.ShouldBeNull());
    }

    // Given a coupon allowing two ticket types
    // When it is redeemed with a selection containing both
    // Then both ticket types are granted and the coupon is marked redeemed
    [TestMethod]
    public void Redeem_SelectionContainsAllCouponTicketTypes_GrantsAllAndMarksRedeemed()
    {
        // Arrange
        var sut = MultiTicketTypeCoupon();

        // Act
        var granted = sut.Redeem(CouponBuilder.DefaultEmail, [WorkshopA, WorkshopB], CouponBuilder.DefaultNow);

        // Assert
        granted.ShouldBe([WorkshopA, WorkshopB], ignoreOrder: true);
        sut.GetStatus(CouponBuilder.DefaultNow).ShouldBe(CouponStatus.Redeemed);
    }

    // Given a coupon allowing two ticket types
    // When it is redeemed with a selection containing only one of them
    // Then only that ticket type is granted, the other is forfeited, and the coupon is fully redeemed
    [TestMethod]
    public void Redeem_SelectionContainsSomeCouponTicketTypes_GrantsOverlapAndForfeitsRest()
    {
        // Arrange
        var sut = MultiTicketTypeCoupon();

        // Act
        var granted = sut.Redeem(CouponBuilder.DefaultEmail, [WorkshopB], CouponBuilder.DefaultNow);

        // Assert
        granted.ShouldBe([WorkshopB]);
        sut.GetStatus(CouponBuilder.DefaultNow).ShouldBe(CouponStatus.Redeemed);

        var second = ErrorResult.Capture(
            () => sut.Redeem(CouponBuilder.DefaultEmail, [WorkshopA], CouponBuilder.DefaultNow));
        second.Error.ShouldMatch(Coupon.Errors.AlreadyRedeemed);
    }

    // Given a coupon allowing one ticket type
    // When it is redeemed with a selection that also holds a ticket type the coupon does not cover
    // Then only the coupon's own ticket type is granted
    [TestMethod]
    public void Redeem_SelectionContainsOtherTicketTypes_GrantsOnlyCouponTicketTypes()
    {
        // Arrange
        var sut = new CouponBuilder().Build();
        var otherTicketTypeId = TicketTypeId.New();

        // Act
        var granted = sut.Redeem(
            CouponBuilder.DefaultEmail,
            [otherTicketTypeId, CouponBuilder.DefaultTicketTypeId],
            CouponBuilder.DefaultNow);

        // Assert
        granted.ShouldBe([CouponBuilder.DefaultTicketTypeId]);
    }

    // Given a coupon of either source allowing two ticket types
    // When it is redeemed with a selection that includes none of them
    // Then it fails with a no-coupon-ticket-type-selected error and the coupon stays active
    [TestMethod]
    [DataRow(CouponSource.Organiser)]
    [DataRow(CouponSource.Waitlist)]
    public void Redeem_SelectionContainsNoCouponTicketType_ThrowsNoCouponTicketTypeSelected(CouponSource source)
    {
        // Arrange
        var sut = MultiTicketTypeCoupon(source);

        // Act
        var result = ErrorResult.Capture(
            () => sut.Redeem(CouponBuilder.DefaultEmail, [TicketTypeId.New()], CouponBuilder.DefaultNow));

        // Assert
        result.Error.ShouldMatch(Coupon.Errors.NoCouponTicketTypeSelected([WorkshopA.Value, WorkshopB.Value]));
        sut.GetStatus(CouponBuilder.DefaultNow).ShouldBe(CouponStatus.Active);
    }

    // Given a waitlist-sourced coupon allowing two ticket types
    // When it is redeemed with a selection containing one of them
    // Then it follows the same partial-tolerant rule as an organiser coupon
    [TestMethod]
    public void Redeem_WaitlistSourcedMultiTicketTypeCoupon_GrantsOverlap()
    {
        // Arrange
        var sut = MultiTicketTypeCoupon(CouponSource.Waitlist);

        // Act
        var granted = sut.Redeem(CouponBuilder.DefaultEmail, [WorkshopA], CouponBuilder.DefaultNow);

        // Assert
        granted.ShouldBe([WorkshopA]);
        sut.GetStatus(CouponBuilder.DefaultNow).ShouldBe(CouponStatus.Redeemed);
    }

    // Given a coupon issued to one email address
    // When it is redeemed by a different email address
    // Then it fails with an email-mismatch error
    [TestMethod]
    public void Redeem_DifferentEmail_ThrowsEmailMismatch()
    {
        // Arrange
        var sut = new CouponBuilder().Build();

        // Act
        var result = ErrorResult.Capture(() => sut.Redeem(
            EmailAddress.From("someone-else@example.com"),
            [CouponBuilder.DefaultTicketTypeId],
            CouponBuilder.DefaultNow));

        // Assert
        result.Error.ShouldMatch(Coupon.Errors.EmailMismatch);
    }

    // Given a coupon allowing two ticket types
    // When it is checked against a selection holding a ticket type outside its allow-list
    // Then it fails with a ticket-type-not-allowed error naming that ticket type
    [TestMethod]
    public void EnsureAllowsAll_SelectionOutsideAllowList_ThrowsTicketTypeNotAllowlisted()
    {
        // Arrange
        var sut = MultiTicketTypeCoupon();
        var otherTicketTypeId = TicketTypeId.New();

        // Act
        var result = ErrorResult.Capture(() => sut.EnsureAllowsAll([WorkshopA, otherTicketTypeId]));

        // Assert
        result.Error.ShouldMatch(Coupon.Errors.TicketTypeNotAllowlisted([otherTicketTypeId.Value]));
    }

    // Given a coupon of a given source
    // When its redemption claim mode is read
    // Then waitlist coupons re-fill the public pool and organiser coupons draw on the reserved buffer
    [TestMethod]
    [DataRow(CouponSource.Waitlist, ClaimMode.PublicUncapped)]
    [DataRow(CouponSource.Organiser, ClaimMode.Reserved)]
    public void RedemptionClaimMode_BySource_ReturnsCapacityPool(CouponSource source, ClaimMode expected)
    {
        // Arrange
        var sut = new CouponBuilder().WithSource(source).Build();

        // Act & Assert
        sut.RedemptionClaimMode.ShouldBe(expected);
    }

    private static readonly TicketTypeId WorkshopA = TicketTypeId.From(new Guid("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"));
    private static readonly TicketTypeId WorkshopB = TicketTypeId.From(new Guid("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"));

    private static Coupon MultiTicketTypeCoupon(CouponSource source = CouponSource.Organiser) =>
        new CouponBuilder()
            .WithRequestedTicketTypeIds(WorkshopA, WorkshopB)
            .WithAvailableTicketTypes(new TicketTypeInfo(WorkshopA), new TicketTypeInfo(WorkshopB))
            .WithSource(source)
            .Build();

    private static void SetRedeemedAt(Coupon coupon, DateTimeOffset redeemedAt)
    {
        var property = typeof(Coupon).GetProperty(nameof(Coupon.RedeemedAt))!;
        property.SetValue(coupon, redeemedAt);
    }
}
