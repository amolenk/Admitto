using Amolenk.Admitto.Core.Registrations.Domain.DomainEvents;
using Amolenk.Admitto.Core.Registrations.Domain.Entities;
using Amolenk.Admitto.Core.Registrations.Domain.ValueObjects;
using Amolenk.Admitto.Core.Shared.Kernel.ErrorHandling;
using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;
using Amolenk.Admitto.Testing.Infrastructure.Assertions;
using Shouldly;

namespace Amolenk.Admitto.Core.Registrations.Domain.Tests.Entities;

[TestClass]
public sealed class WaitlistTests
{
    private static readonly TicketedEventId DefaultEventId = TicketedEventId.New();
    private static readonly TicketTypeId DefaultTicketTypeId = TicketTypeId.New();
    private static readonly TeamId DefaultTeamId = TeamId.New();

    private static Waitlist CreateWaitlist() =>
        Waitlist.Create(DefaultEventId, DefaultTicketTypeId, DefaultTeamId);

    private static TicketedEvent CreateTicketedEvent()
    {
        var now = DateTimeOffset.UtcNow;
        return TicketedEvent.Create(
            CreationRequestId.From(Guid.NewGuid()),
            DefaultEventId, DefaultTeamId,
            EventName.From("Test Event"),
            AbsoluteUrl.From("https://example.com"),
            AbsoluteUrl.From("https://tickets.example.com"),
            now.AddDays(10), now.AddDays(11),
            TimeZoneId.From("UTC"));
    }

    private static TicketType CreateTicketType()
    {
        var catalog = TicketCatalog.Create(DefaultEventId);
        catalog.AddTicketType(DefaultTicketTypeId, TicketTypeName.From("Conference Pass"), [], maxCapacity: 100);
        return catalog.TicketTypes.Single(tt => tt.Id == DefaultTicketTypeId);
    }

    [TestMethod]
    public void RequestJoin_WhenEmailIsNew_RaisesWaitlistEntryAddedDomainEvent()
    {
        // Arrange
        var sut = CreateWaitlist();
        var email = EmailAddress.From("alice@example.com");

        // Act
        var result = sut.RequestJoin(email);

        // Assert
        result.ShouldBeTrue();
        sut.GetDomainEvents()
            .ShouldHaveSingleItem()
            .ShouldBeAssignableTo<WaitlistEntryAddedDomainEvent>()
            .ShouldSatisfyAllConditions(
                e => e.Email.ShouldBe(email),
                e => e.TicketTypeId.ShouldBe(DefaultTicketTypeId),
                e => e.TicketedEventId.ShouldBe(DefaultEventId));
    }

    [TestMethod]
    public void RequestJoin_WhenEmailAlreadyActive_ReturnsFalseWithNoEvent()
    {
        // Arrange
        var sut = CreateWaitlist();
        var email = EmailAddress.From("alice@example.com");
        sut.ConfirmEntry(email, DateTimeOffset.UtcNow);
        sut.ClearDomainEvents();

        // Act
        var result = sut.RequestJoin(email);

        // Assert
        result.ShouldBeFalse();
        sut.GetDomainEvents().ShouldBeEmpty();
    }

    [TestMethod]
    public void ConfirmEntry_WhenNewEmail_AddsActiveEntryAtNextPosition()
    {
        // Arrange
        var sut = CreateWaitlist();
        sut.ConfirmEntry(EmailAddress.From("alice@example.com"), DateTimeOffset.UtcNow);

        // Act
        sut.ConfirmEntry(EmailAddress.From("bob@example.com"), DateTimeOffset.UtcNow);

        // Assert
        sut.Entries.Count.ShouldBe(2);
        sut.Entries[0].Position.ShouldBe(1);
        sut.Entries[1].Position.ShouldBe(2);
        sut.Entries.ShouldAllBe(e => e.Status == WaitlistEntryStatus.Active);
    }

    [TestMethod]
    public void ConfirmEntry_WhenAlreadyConfirmed_IsIdempotent()
    {
        // Arrange
        var sut = CreateWaitlist();
        var email = EmailAddress.From("alice@example.com");
        sut.ConfirmEntry(email, DateTimeOffset.UtcNow);

        // Act
        sut.ConfirmEntry(email, DateTimeOffset.UtcNow);

        // Assert
        sut.Entries.Count(e => e.Email == email && e.Status == WaitlistEntryStatus.Active).ShouldBe(1);
    }

    [TestMethod]
    public void RemoveEntry_ByEmail_MarksEntryRemovedAndRenumbersPositions()
    {
        // Arrange
        var sut = CreateWaitlist();
        var alice = EmailAddress.From("alice@example.com");
        var bob = EmailAddress.From("bob@example.com");
        sut.ConfirmEntry(alice, DateTimeOffset.UtcNow);
        sut.ConfirmEntry(bob, DateTimeOffset.UtcNow);
        sut.ClearDomainEvents();

        // Act
        sut.RemoveEntry(alice);

        // Assert
        sut.Entries.First(e => e.Email == alice).Status.ShouldBe(WaitlistEntryStatus.Removed);
        sut.Entries.First(e => e.Email == bob).Position.ShouldBe(1);
        sut.GetDomainEvents()
            .ShouldHaveSingleItem()
            .ShouldBeAssignableTo<WaitlistEntryRemovedDomainEvent>();
    }

    [TestMethod]
    public void RemoveEntry_ByEmail_WhenNotFound_IsIdempotent()
    {
        // Arrange
        var sut = CreateWaitlist();
        sut.ClearDomainEvents();

        // Act & Assert — no exception raised
        sut.RemoveEntry(EmailAddress.From("nobody@example.com"));
        sut.GetDomainEvents().ShouldBeEmpty();
    }

    [TestMethod]
    public void RemoveEntry_ByEntryId_WhenEntryAlreadyRemoved_IsIdempotent()
    {
        // Arrange
        var sut = CreateWaitlist();
        var email = EmailAddress.From("alice@example.com");
        sut.ConfirmEntry(email, DateTimeOffset.UtcNow);
        var entryId = sut.Entries.Single().Id;
        sut.RemoveEntry(email);
        sut.ClearDomainEvents();

        // Act & Assert — no exception, no extra events
        sut.RemoveEntry(entryId);
        sut.GetDomainEvents().ShouldBeEmpty();
    }

    [TestMethod]
    public void RemoveEntry_ByEntryId_WhenNotFound_ThrowsEntryNotFoundError()
    {
        // Arrange
        var sut = CreateWaitlist();

        // Act & Assert
        Should.Throw<BusinessRuleViolationException>(() =>
            sut.RemoveEntry(WaitlistEntryId.New()));
    }

    [TestMethod]
    public void CheckExhausted_WhenEntriesAndCouponsAllGone_RaisesWaitlistExhaustedDomainEvent()
    {
        // Arrange
        var sut = CreateWaitlist();
        var email = EmailAddress.From("alice@example.com");
        sut.ConfirmEntry(email, DateTimeOffset.UtcNow);
        sut.ClearDomainEvents();

        // Act
        sut.RemoveEntry(email); // triggers CheckExhausted with no entries or coupons

        // Assert
        sut.GetDomainEvents()
            .OfType<WaitlistExhaustedDomainEvent>()
            .ShouldHaveSingleItem()
            .ShouldSatisfyAllConditions(
                e => e.TicketedEventId.ShouldBe(DefaultEventId),
                e => e.TicketTypeId.ShouldBe(DefaultTicketTypeId));
    }

    [TestMethod]
    public void CheckExhausted_WhenIssuedCouponsRemain_DoesNotRaiseWaitlistExhaustedDomainEvent()
    {
        // Arrange
        var sut = CreateWaitlist();
        var email = EmailAddress.From("alice@example.com");
        sut.ConfirmEntry(email, DateTimeOffset.UtcNow);
        sut.TrackIssuedCoupon(CouponId.New(), DateTimeOffset.UtcNow);
        sut.ClearDomainEvents();

        // Act
        sut.RemoveEntry(email); // entries gone but coupon still issued

        // Assert
        sut.GetDomainEvents().OfType<WaitlistExhaustedDomainEvent>().ShouldBeEmpty();
    }

    [TestMethod]
    public void RedeemCoupon_TransitionsStatusToRedeemed()
    {
        // Arrange
        var sut = CreateWaitlist();
        var couponId = CouponId.New();
        sut.TrackIssuedCoupon(couponId, DateTimeOffset.UtcNow);

        // Act
        sut.RedeemCoupon(couponId);

        // Assert
        sut.Coupons.Single().Status.ShouldBe(WaitlistCouponStatus.Redeemed);
    }

    [TestMethod]
    public void RevokeCoupon_TransitionsStatusToRevoked()
    {
        // Arrange
        var sut = CreateWaitlist();
        var couponId = CouponId.New();
        sut.TrackIssuedCoupon(couponId, DateTimeOffset.UtcNow);

        // Act
        sut.RevokeCoupon(couponId);

        // Assert
        sut.Coupons.Single().Status.ShouldBe(WaitlistCouponStatus.Revoked);
    }

    [TestMethod]
    public void IssueNextCoupon_WhenActiveEntryExists_RemovesTopEntryAndReturnsCoupon()
    {
        // Arrange
        var sut = CreateWaitlist();
        var email = EmailAddress.From("alice@example.com");
        sut.ConfirmEntry(email, DateTimeOffset.UtcNow);
        sut.ClearDomainEvents();

        // Act
        var result = sut.IssueNextCoupon(CreateTicketedEvent(), CreateTicketType(), DateTimeOffset.UtcNow);
        sut.Entries.ShouldNotContain(e => e.Status == WaitlistEntryStatus.Active);
        sut.Coupons.ShouldHaveSingleItem().Id.ShouldBe(result.Id);
    }

    [TestMethod]
    public void IssueNextCoupon_IssuesInPositionOrder_WhenMultipleEntries()
    {
        // Arrange
        var sut = CreateWaitlist();
        var now = DateTimeOffset.UtcNow;
        // first@example.com gets position 1, second@example.com gets position 2
        sut.ConfirmEntry(EmailAddress.From("first@example.com"), now);
        sut.ConfirmEntry(EmailAddress.From("second@example.com"), now.AddMinutes(1));
        sut.ClearDomainEvents();

        // Act
        var result = sut.IssueNextCoupon(CreateTicketedEvent(), CreateTicketType(), now);
        result.ShouldNotBeNull();
        result.Email.Value.ShouldBe("first@example.com");
    }

    [TestMethod]
    public void IssueNextCoupon_WhenNoActiveEntries_ReturnsNull()
    {
        // Arrange
        var sut = CreateWaitlist();
        sut.ClearDomainEvents();

        // Act
        var result = sut.IssueNextCoupon(CreateTicketedEvent(), CreateTicketType(), DateTimeOffset.UtcNow);
        sut.GetDomainEvents().ShouldBeEmpty();
        sut.Coupons.ShouldBeEmpty();
    }

    [TestMethod]
    public void RedeemCoupon_WhenCouponAlreadyRedeemed_ThrowsConflictError()
    {
        // Arrange
        var sut = CreateWaitlist();
        var couponId = CouponId.New();
        sut.TrackIssuedCoupon(couponId, DateTimeOffset.UtcNow);
        sut.RedeemCoupon(couponId);

        // Act & Assert — second redemption attempt must fail, not silently overwrite
        Should.Throw<BusinessRuleViolationException>(() => sut.RedeemCoupon(couponId));
    }

    [TestMethod]
    public void RedeemCoupon_WhenCouponAlreadyRevoked_ThrowsConflictError()
    {
        // Arrange — simulates the race-loser scenario: expiry job revoked first, attendee redeems second
        var sut = CreateWaitlist();
        var couponId = CouponId.New();
        sut.TrackIssuedCoupon(couponId, DateTimeOffset.UtcNow);
        sut.RevokeCoupon(couponId);

        // Act & Assert — the EF Core concurrency token (xmin) is the first guard; this is the
        // fallback guard for in-memory consistency.
        Should.Throw<BusinessRuleViolationException>(() => sut.RedeemCoupon(couponId));
    }

    [TestMethod]
    public void RevokeCoupon_WhenCouponAlreadyRedeemed_ThrowsConflictError()
    {
        // Arrange — simulates the race-loser scenario: attendee redeemed first, expiry job revokes second
        var sut = CreateWaitlist();
        var couponId = CouponId.New();
        sut.TrackIssuedCoupon(couponId, DateTimeOffset.UtcNow);
        sut.RedeemCoupon(couponId);

        // Act & Assert
        Should.Throw<BusinessRuleViolationException>(() => sut.RevokeCoupon(couponId));
    }

    [TestMethod]
    public void RevokeCoupon_WhenCouponAlreadyRevoked_ThrowsConflictError()
    {
        // Arrange
        var sut = CreateWaitlist();
        var couponId = CouponId.New();
        sut.TrackIssuedCoupon(couponId, DateTimeOffset.UtcNow);
        sut.RevokeCoupon(couponId);

        // Act & Assert
        Should.Throw<BusinessRuleViolationException>(() => sut.RevokeCoupon(couponId));
    }
}
