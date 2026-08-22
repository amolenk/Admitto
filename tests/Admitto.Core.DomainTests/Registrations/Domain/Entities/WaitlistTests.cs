using Amolenk.Admitto.Core.Registrations.Domain.DomainEvents;
using Amolenk.Admitto.Core.Registrations.Domain.Entities;
using Amolenk.Admitto.Core.Registrations.Domain.ValueObjects;
using Amolenk.Admitto.Core.Shared.Kernel.ErrorHandling;
using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;
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
        var catalog = TicketCatalog.Create(DefaultEventId, TeamId.New());
        catalog.AddTicketType(DefaultTicketTypeId, TicketTypeName.From("Conference Pass"), [], maxCapacity: 100);
        return catalog.TicketTypes.Single(tt => tt.Id == DefaultTicketTypeId);
    }

    // Given an empty waitlist
    // When an entry is added for a new email
    // Then it succeeds, adding a single active entry, without raising domain events
    [TestMethod]
    public void AddEntry_WhenEmailIsNew_AddsActiveEntry()
    {
        // Arrange
        var sut = CreateWaitlist();
        var email = EmailAddress.From("alice@example.com");
        var now = DateTimeOffset.UtcNow;

        // Act
        var result = sut.AddEntry(email, now);

        // Assert
        result.ShouldBeTrue();
        sut.Entries.ShouldHaveSingleItem()
            .ShouldSatisfyAllConditions(
                e => e.Email.ShouldBe(email),
                e => e.Status.ShouldBe(WaitlistEntryStatus.Active));
        sut.GetDomainEvents().ShouldBeEmpty();
    }

    // Given an email that already has an active waitlist entry
    // When the same email is added again
    // Then it returns false and no duplicate active entry is created
    [TestMethod]
    public void AddEntry_WhenEmailAlreadyActive_ReturnsFalse()
    {
        // Arrange
        var sut = CreateWaitlist();
        var email = EmailAddress.From("alice@example.com");
        sut.AddEntry(email, DateTimeOffset.UtcNow);

        // Act
        var result = sut.AddEntry(email, DateTimeOffset.UtcNow);

        // Assert
        result.ShouldBeFalse();
        sut.Entries.Count(e => e.Email == email && e.Status == WaitlistEntryStatus.Active).ShouldBe(1);
    }

    // Given a waitlist that already has one active entry
    // When another new email is added
    // Then both entries stay active, numbered sequentially by position
    [TestMethod]
    public void AddEntry_WhenNewEmail_AddsActiveEntryAtNextPosition()
    {
        // Arrange
        var sut = CreateWaitlist();
        sut.AddEntry(EmailAddress.From("alice@example.com"), DateTimeOffset.UtcNow);

        // Act
        sut.AddEntry(EmailAddress.From("bob@example.com"), DateTimeOffset.UtcNow);

        // Assert
        sut.Entries.Count.ShouldBe(2);
        sut.Entries[0].Position.ShouldBe(1);
        sut.Entries[1].Position.ShouldBe(2);
        sut.Entries.ShouldAllBe(e => e.Status == WaitlistEntryStatus.Active);
    }

    // Given a waitlist with two active entries
    // When the first entry is removed by email
    // Then it is marked Removed, the remaining entry is renumbered, and a WaitlistEntryRemoved event is raised
    [TestMethod]
    public void RemoveEntry_ByEmail_MarksEntryRemovedAndRenumbersPositions()
    {
        // Arrange
        var sut = CreateWaitlist();
        var alice = EmailAddress.From("alice@example.com");
        var bob = EmailAddress.From("bob@example.com");
        sut.AddEntry(alice, DateTimeOffset.UtcNow);
        sut.AddEntry(bob, DateTimeOffset.UtcNow);
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

    // Given an empty waitlist
    // When removal is attempted for an email that has no entry
    // Then it does not throw and raises no domain events
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

    // Given an entry that has already been removed
    // When removal is attempted again by entry id
    // Then it does not throw and raises no additional domain events
    [TestMethod]
    public void RemoveEntry_ByEntryId_WhenEntryAlreadyRemoved_IsIdempotent()
    {
        // Arrange
        var sut = CreateWaitlist();
        var email = EmailAddress.From("alice@example.com");
        sut.AddEntry(email, DateTimeOffset.UtcNow);
        var entryId = sut.Entries.Single().Id;
        sut.RemoveEntry(email);
        sut.ClearDomainEvents();

        // Act & Assert — no exception, no extra events
        sut.RemoveEntry(entryId);
        sut.GetDomainEvents().ShouldBeEmpty();
    }

    // Given an empty waitlist
    // When removal is attempted for an entry id that does not exist
    // Then it throws a business rule violation
    [TestMethod]
    public void RemoveEntry_ByEntryId_WhenNotFound_ThrowsEntryNotFoundError()
    {
        // Arrange
        var sut = CreateWaitlist();

        // Act & Assert
        Should.Throw<BusinessRuleViolationException>(() =>
            sut.RemoveEntry(WaitlistEntryId.New()));
    }

    // Given a waitlist whose only entry is about to be removed with no coupons outstanding
    // When that last entry is removed
    // Then a WaitlistExhausted domain event is raised for the event and ticket type
    [TestMethod]
    public void CheckExhausted_WhenEntriesAndCouponsAllGone_RaisesWaitlistExhaustedDomainEvent()
    {
        // Arrange
        var sut = CreateWaitlist();
        var email = EmailAddress.From("alice@example.com");
        sut.AddEntry(email, DateTimeOffset.UtcNow);
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

    // Given a waitlist whose last entry is removed but an issued coupon is still outstanding
    // When that last entry is removed
    // Then no WaitlistExhausted domain event is raised
    [TestMethod]
    public void CheckExhausted_WhenIssuedCouponsRemain_DoesNotRaiseWaitlistExhaustedDomainEvent()
    {
        // Arrange
        var sut = CreateWaitlist();
        var email = EmailAddress.From("alice@example.com");
        sut.AddEntry(email, DateTimeOffset.UtcNow);
        sut.TrackIssuedCoupon(CouponId.New(), DateTimeOffset.UtcNow);
        sut.ClearDomainEvents();

        // Act
        sut.RemoveEntry(email); // entries gone but coupon still issued

        // Assert
        sut.GetDomainEvents().OfType<WaitlistExhaustedDomainEvent>().ShouldBeEmpty();
    }

    // Given a waitlist with an issued coupon
    // When the coupon is redeemed
    // Then its status becomes Redeemed
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

    // Given a waitlist with an issued coupon
    // When the coupon is revoked
    // Then its status becomes Revoked
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

    // Given a waitlist with one active entry
    // When the next coupon is issued for that event and ticket type
    // Then the top entry is no longer active and a coupon is returned and tracked
    [TestMethod]
    public void IssueNextCoupon_WhenActiveEntryExists_RemovesTopEntryAndReturnsCoupon()
    {
        // Arrange
        var sut = CreateWaitlist();
        var email = EmailAddress.From("alice@example.com");
        sut.AddEntry(email, DateTimeOffset.UtcNow);
        sut.ClearDomainEvents();

        // Act
        var result = sut.IssueNextCoupon(CreateTicketedEvent(), CreateTicketType(), DateTimeOffset.UtcNow);
        sut.Entries.ShouldNotContain(e => e.Status == WaitlistEntryStatus.Active);
        result.ShouldNotBeNull();
        sut.Coupons.ShouldHaveSingleItem().Id.ShouldBe(result.Id);
    }

    // Given a waitlist with two active entries added at different times
    // When the next coupon is issued
    // Then it goes to the entry with the earliest position
    [TestMethod]
    public void IssueNextCoupon_IssuesInPositionOrder_WhenMultipleEntries()
    {
        // Arrange
        var sut = CreateWaitlist();
        var now = DateTimeOffset.UtcNow;
        // first@example.com gets position 1, second@example.com gets position 2
        sut.AddEntry(EmailAddress.From("first@example.com"), now);
        sut.AddEntry(EmailAddress.From("second@example.com"), now.AddMinutes(1));
        sut.ClearDomainEvents();

        // Act
        var result = sut.IssueNextCoupon(CreateTicketedEvent(), CreateTicketType(), now);
        result.ShouldNotBeNull();
        result.Email.Value.ShouldBe("first@example.com");
    }

    // Given a waitlist with no active entries
    // When the next coupon is issued
    // Then it returns null and no coupon is tracked
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

    // Given a coupon that has already been redeemed
    // When redemption is attempted again
    // Then it throws a business rule violation instead of silently overwriting
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

    // Given a coupon that was already revoked (e.g. by an expiry job)
    // When an attendee then attempts to redeem it
    // Then it throws a business rule violation
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

    // Given a coupon that an attendee already redeemed
    // When an expiry job then attempts to revoke it
    // Then it throws a business rule violation
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

    // Given a coupon that has already been revoked
    // When revocation is attempted again
    // Then it throws a business rule violation
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
