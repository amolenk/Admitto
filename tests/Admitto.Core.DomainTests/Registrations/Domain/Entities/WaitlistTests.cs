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
    private static readonly EmailAddress RedeemerEmail = EmailAddress.From("redeemer@example.com");

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

    private static TicketCatalog CreateCatalog()
    {
        var catalog = TicketCatalog.Create(DefaultEventId, DefaultTeamId);
        catalog.AddTicketType(DefaultTicketTypeId, TicketTypeName.From("Conference Pass"), [], maxCapacity: 100);
        return catalog;
    }

    private static TicketType TicketTypeOf(TicketCatalog catalog) => catalog.FindTicketType(DefaultTicketTypeId);

    /// <summary>
    /// The catalog every entry in a test joins and leaves, and every offer holds a seat on (a fresh one per test).
    /// </summary>
    private readonly TicketCatalog _catalog = CreateCatalog();

    /// <summary>
    /// Queues a fresh entry and issues it the next coupon, so the waitlist tracks one outstanding coupon.
    /// </summary>
    private Coupon IssueCoupon(Waitlist sut)
    {
        sut.AddEntry(EmailAddress.From($"{Guid.NewGuid():N}@example.com"), DateTimeOffset.UtcNow, _catalog);
        return sut.IssueNextCoupon(CreateTicketedEvent(), _catalog, DateTimeOffset.UtcNow)!;
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
        var result = sut.AddEntry(email, now, _catalog);

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
        sut.AddEntry(email, DateTimeOffset.UtcNow, _catalog);

        // Act
        var result = sut.AddEntry(email, DateTimeOffset.UtcNow, _catalog);

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
        sut.AddEntry(EmailAddress.From("alice@example.com"), DateTimeOffset.UtcNow, _catalog);

        // Act
        sut.AddEntry(EmailAddress.From("bob@example.com"), DateTimeOffset.UtcNow, _catalog);

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
        sut.AddEntry(alice, DateTimeOffset.UtcNow, _catalog);
        sut.AddEntry(bob, DateTimeOffset.UtcNow, _catalog);
        sut.ClearDomainEvents();

        // Act
        sut.RemoveEntry(alice, _catalog);

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
        sut.RemoveEntry(EmailAddress.From("nobody@example.com"), _catalog);
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
        sut.AddEntry(email, DateTimeOffset.UtcNow, _catalog);
        var entryId = sut.Entries.Single().Id;
        sut.RemoveEntry(email, _catalog);
        sut.ClearDomainEvents();

        // Act & Assert — no exception, no extra events
        sut.RemoveEntry(entryId, _catalog);
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
            sut.RemoveEntry(WaitlistEntryId.New(), _catalog));
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
        sut.AddEntry(email, DateTimeOffset.UtcNow, _catalog);
        sut.ClearDomainEvents();

        // Act
        sut.RemoveEntry(email, _catalog); // triggers CheckExhausted with no entries or coupons

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
        IssueCoupon(sut);
        var email = EmailAddress.From("alice@example.com");
        sut.AddEntry(email, DateTimeOffset.UtcNow, _catalog);
        sut.ClearDomainEvents();

        // Act
        sut.RemoveEntry(email, _catalog); // entries gone but coupon still issued

        // Assert
        sut.GetDomainEvents().OfType<WaitlistExhaustedDomainEvent>().ShouldBeEmpty();
    }

    // Given a waitlist with an issued coupon
    // When the coupon is redeemed
    // Then its status becomes Redeemed
    [TestMethod]
    public void ApplyCouponRedemption_TransitionsStatusToRedeemed()
    {
        // Arrange
        var sut = CreateWaitlist();
        var couponId = IssueCoupon(sut).Id;

        // Act
        sut.ApplyCouponRedemption(couponId, RedeemerEmail, _catalog);

        // Assert
        sut.Coupons.Single().Status.ShouldBe(WaitlistCouponStatus.Redeemed);
    }

    // Given a waitlist with one active entry
    // When the next coupon is issued
    // Then the tracked waitlist coupon carries the coupon's expiry
    [TestMethod]
    public void IssueNextCoupon_WhenActiveEntryExists_TracksCouponExpiry()
    {
        // Arrange
        var sut = CreateWaitlist();
        sut.AddEntry(EmailAddress.From("alice@example.com"), DateTimeOffset.UtcNow, _catalog);

        // Act
        var coupon = sut.IssueNextCoupon(CreateTicketedEvent(), _catalog, DateTimeOffset.UtcNow)!;

        // Assert
        sut.Coupons.ShouldHaveSingleItem().ExpiresAt.ShouldBe(coupon.ExpiresAt);
    }

    // Given a waitlist with an active entry
    // When an organizer promotes that entry directly
    // Then the tracked waitlist coupon carries the coupon's expiry
    [TestMethod]
    public void IssueCouponToEntry_ActiveEntry_TracksCouponExpiry()
    {
        // Arrange
        var sut = CreateWaitlist();
        sut.AddEntry(EmailAddress.From("alice@example.com"), DateTimeOffset.UtcNow, _catalog);
        var entryId = sut.Entries.Single().Id;

        // Act
        var coupon = sut.IssueCouponToEntry(entryId, CreateTicketedEvent(), _catalog, DateTimeOffset.UtcNow);

        // Assert
        sut.Coupons.ShouldHaveSingleItem().ExpiresAt.ShouldBe(coupon.ExpiresAt);
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
        sut.AddEntry(email, DateTimeOffset.UtcNow, _catalog);
        sut.ClearDomainEvents();

        // Act
        var result = sut.IssueNextCoupon(CreateTicketedEvent(), _catalog, DateTimeOffset.UtcNow);
        sut.Entries.ShouldNotContain(e => e.Status == WaitlistEntryStatus.Active);
        result.ShouldNotBeNull();
        sut.Coupons.ShouldHaveSingleItem().Id.ShouldBe(result.Id);
    }

    // Given a waitlist with one active entry
    // When the next coupon is issued for that entry
    // Then a WaitlistCouponIssued domain event is raised with the recipient, coupon code, ticket type name, and expiry
    [TestMethod]
    public void IssueNextCoupon_WhenActiveEntryExists_RaisesWaitlistCouponIssuedDomainEvent()
    {
        // Arrange
        var sut = CreateWaitlist();
        var email = EmailAddress.From("alice@example.com");
        sut.AddEntry(email, DateTimeOffset.UtcNow, _catalog);
        sut.ClearDomainEvents();
        var catalog = _catalog;
        var now = DateTimeOffset.UtcNow;

        // Act
        var result = sut.IssueNextCoupon(CreateTicketedEvent(), catalog, now);

        // Assert
        result.ShouldNotBeNull();
        sut.GetDomainEvents()
            .OfType<WaitlistCouponIssuedDomainEvent>()
            .ShouldHaveSingleItem()
            .ShouldSatisfyAllConditions(
                e => e.TeamId.ShouldBe(DefaultTeamId),
                e => e.TicketedEventId.ShouldBe(DefaultEventId),
                e => e.TicketTypeId.ShouldBe(DefaultTicketTypeId),
                e => e.RecipientEmail.ShouldBe(email),
                e => e.CouponCode.ShouldBe(result.Code),
                e => e.TicketTypeName.ShouldBe(TicketTypeOf(catalog).Name.Value),
                e => e.ExpiresAt.ShouldBe(result.ExpiresAt));
    }

    // Given a waitlist coupon that was issued to the front-of-queue attendee
    // When that coupon expires unclaimed
    // Then the waitlist coupon is expired and a WaitlistCouponExpired event is raised for that attendee
    [TestMethod]
    public void ExpireCoupon_WhenCouponIssued_ExpiresAndRaisesWaitlistCouponExpiredDomainEvent()
    {
        // Arrange
        var sut = CreateWaitlist();
        var email = EmailAddress.From("alice@example.com");
        sut.AddEntry(email, DateTimeOffset.UtcNow, _catalog);
        var catalog = _catalog;
        var coupon = sut.IssueNextCoupon(CreateTicketedEvent(), catalog, DateTimeOffset.UtcNow)!;
        sut.ClearDomainEvents();

        // Act
        sut.ExpireCoupon(coupon.Id, coupon, catalog);

        // Assert
        sut.Coupons.ShouldHaveSingleItem().Status.ShouldBe(WaitlistCouponStatus.Expired);
        sut.GetDomainEvents()
            .OfType<WaitlistCouponExpiredDomainEvent>()
            .ShouldHaveSingleItem()
            .ShouldSatisfyAllConditions(
                e => e.TeamId.ShouldBe(DefaultTeamId),
                e => e.TicketedEventId.ShouldBe(DefaultEventId),
                e => e.TicketTypeId.ShouldBe(DefaultTicketTypeId),
                e => e.RecipientEmail.ShouldBe(email),
                e => e.CouponCode.ShouldBe(coupon.Code),
                e => e.TicketTypeName.ShouldBe(TicketTypeOf(catalog).Name.Value));
    }

    // Given a waitlist coupon that has already been redeemed
    // When an expiry is attempted for that coupon
    // Then it throws the coupon-not-expirable error and no expired event is raised
    [TestMethod]
    public void ExpireCoupon_WhenCouponAlreadyRedeemed_ThrowsAndRaisesNoExpiredEvent()
    {
        // Arrange
        var sut = CreateWaitlist();
        sut.AddEntry(EmailAddress.From("alice@example.com"), DateTimeOffset.UtcNow, _catalog);
        var catalog = _catalog;
        var coupon = sut.IssueNextCoupon(CreateTicketedEvent(), catalog, DateTimeOffset.UtcNow)!;
        sut.ApplyCouponRedemption(coupon.Id, RedeemerEmail, _catalog);
        sut.ClearDomainEvents();

        // Act
        var result = ErrorResult.Capture(() => sut.ExpireCoupon(coupon.Id, coupon, catalog));

        // Assert
        result.Error.ShouldMatch(WaitlistCoupon.Errors.CouponNotExpirable);
        sut.GetDomainEvents().OfType<WaitlistCouponExpiredDomainEvent>().ShouldBeEmpty();
    }

    // Given an automatically issued waitlist coupon whose ticket type no longer exists
    // When that coupon expires unclaimed
    // Then the waitlist coupon is expired, but no expired-offer event is raised
    [TestMethod]
    public void ExpireCoupon_TicketTypeMissing_ExpiresWithoutExpiredEvent()
    {
        // Arrange
        var sut = CreateWaitlist();
        sut.AddEntry(EmailAddress.From("alice@example.com"), DateTimeOffset.UtcNow, _catalog);
        var coupon = sut.IssueNextCoupon(CreateTicketedEvent(), _catalog, DateTimeOffset.UtcNow)!;
        sut.ClearDomainEvents();
        var catalogWithoutTicketType = TicketCatalog.Create(DefaultEventId, DefaultTeamId);

        // Act
        sut.ExpireCoupon(coupon.Id, coupon, catalogWithoutTicketType);

        // Assert
        sut.Coupons.ShouldHaveSingleItem().Status.ShouldBe(WaitlistCouponStatus.Expired);
        sut.GetDomainEvents().OfType<WaitlistCouponExpiredDomainEvent>().ShouldBeEmpty();
    }

    // Given an automatically issued waitlist coupon whose coupon record is missing
    // When that coupon expires unclaimed
    // Then the waitlist coupon is expired and its hold given back, but no expired-offer event is raised
    [TestMethod]
    public void ExpireCoupon_CouponMissing_ExpiresAndReleasesHoldWithoutExpiredEvent()
    {
        // Arrange
        var sut = CreateWaitlist();
        var catalog = _catalog;
        sut.AddEntry(EmailAddress.From("alice@example.com"), DateTimeOffset.UtcNow, _catalog);
        var couponId = sut.IssueNextCoupon(CreateTicketedEvent(), catalog, DateTimeOffset.UtcNow)!.Id;
        sut.ClearDomainEvents();

        // Act
        sut.ExpireCoupon(couponId, coupon: null, catalog);

        // Assert
        TicketTypeOf(catalog).WaitlistHeldCapacity.ShouldBe(0);
        sut.Coupons.ShouldHaveSingleItem().Status.ShouldBe(WaitlistCouponStatus.Expired);
        sut.GetDomainEvents().OfType<WaitlistCouponExpiredDomainEvent>().ShouldBeEmpty();
    }

    // Given the waitlist's only outstanding coupon, with nobody left in the queue
    // When that coupon expires unclaimed
    // Then a WaitlistExhausted event is raised
    [TestMethod]
    public void ExpireCoupon_LastOutstandingCoupon_RaisesWaitlistExhaustedDomainEvent()
    {
        // Arrange
        var sut = CreateWaitlist();
        var catalog = _catalog;
        sut.AddEntry(EmailAddress.From("alice@example.com"), DateTimeOffset.UtcNow, _catalog);
        var coupon = sut.IssueNextCoupon(CreateTicketedEvent(), catalog, DateTimeOffset.UtcNow)!;
        sut.ClearDomainEvents();

        // Act
        sut.ExpireCoupon(coupon.Id, coupon, catalog);

        // Assert
        sut.GetDomainEvents().OfType<WaitlistExhaustedDomainEvent>().ShouldHaveSingleItem();
    }

    // Given a waitlist with an active entry
    // When the next coupon is issued from the front of the queue
    // Then the tracked coupon is marked as automatically issued
    [TestMethod]
    public void IssueNextCoupon_WhenActiveEntryExists_TracksCouponAsAutomatic()
    {
        // Arrange
        var sut = CreateWaitlist();
        sut.AddEntry(EmailAddress.From("alice@example.com"), DateTimeOffset.UtcNow, _catalog);

        // Act
        sut.IssueNextCoupon(CreateTicketedEvent(), _catalog, DateTimeOffset.UtcNow);

        // Assert
        sut.Coupons.ShouldHaveSingleItem().Origin.ShouldBe(WaitlistCouponOrigin.Automatic);
    }

    // Given a waitlist with an active entry
    // When an organizer promotes that entry directly
    // Then the tracked coupon is marked as manually issued
    [TestMethod]
    public void IssueCouponToEntry_ActiveEntry_TracksCouponAsManual()
    {
        // Arrange
        var sut = CreateWaitlist();
        sut.AddEntry(EmailAddress.From("alice@example.com"), DateTimeOffset.UtcNow, _catalog);
        var entryId = sut.Entries.Single().Id;

        // Act
        sut.IssueCouponToEntry(entryId, CreateTicketedEvent(), _catalog, DateTimeOffset.UtcNow);

        // Assert
        sut.Coupons.ShouldHaveSingleItem().Origin.ShouldBe(WaitlistCouponOrigin.Manual);
    }

    // Given a waitlist with an active entry
    // When the next coupon is issued from the front of the queue
    // Then the offer holds a seat on the ticket type
    [TestMethod]
    public void IssueNextCoupon_WhenActiveEntryExists_HoldsSeatOnTicketType()
    {
        // Arrange
        var sut = CreateWaitlist();
        var catalog = _catalog;
        sut.AddEntry(EmailAddress.From("alice@example.com"), DateTimeOffset.UtcNow, _catalog);

        // Act
        sut.IssueNextCoupon(CreateTicketedEvent(), catalog, DateTimeOffset.UtcNow);

        // Assert
        TicketTypeOf(catalog).WaitlistHeldCapacity.ShouldBe(1);
    }

    // Given a sold-out ticket type with an attendee waiting
    // When an organizer promotes that attendee as a VIP
    // Then the offer still holds a seat, taking the ticket type's availability below zero
    [TestMethod]
    public void IssueCouponToEntry_SoldOut_HoldsSeatBeyondCapacity()
    {
        // Arrange
        var sut = CreateWaitlist();
        var catalog = TicketCatalog.Create(DefaultEventId, DefaultTeamId);
        catalog.AddTicketType(DefaultTicketTypeId, TicketTypeName.From("Conference Pass"), [], maxCapacity: 1,
            waitlistEnabled: true);
        catalog.Claim([DefaultTicketTypeId], ClaimMode.Public);
        sut.AddEntry(EmailAddress.From("alice@example.com"), DateTimeOffset.UtcNow, catalog);

        // Act
        sut.IssueCouponToEntry(sut.Entries.Single().Id, CreateTicketedEvent(), catalog, DateTimeOffset.UtcNow);

        // Assert
        TicketTypeOf(catalog).WaitlistHeldCapacity.ShouldBe(1);
        TicketTypeOf(catalog).AvailableCapacity.ShouldBe(-1);
    }

    // Given an automatic and a VIP waitlist coupon outstanding
    // When both expire unclaimed
    // Then each gives back the seat it held, whatever its origin
    [TestMethod]
    public void ExpireCoupon_AutomaticAndManualCoupons_ReleaseTheirHolds()
    {
        // Arrange
        var sut = CreateWaitlist();
        var catalog = _catalog;
        sut.AddEntry(EmailAddress.From("alice@example.com"), DateTimeOffset.UtcNow, _catalog);
        sut.AddEntry(EmailAddress.From("bob@example.com"), DateTimeOffset.UtcNow, _catalog);
        var automatic = sut.IssueNextCoupon(CreateTicketedEvent(), catalog, DateTimeOffset.UtcNow)!;
        var manual = sut.IssueCouponToEntry(
            sut.Entries.Single(e => e.Status == WaitlistEntryStatus.Active).Id,
            CreateTicketedEvent(), catalog, DateTimeOffset.UtcNow);

        // Act
        sut.ExpireCoupon(automatic.Id, automatic, catalog);
        sut.ExpireCoupon(manual.Id, manual, catalog);

        // Assert
        TicketTypeOf(catalog).WaitlistHeldCapacity.ShouldBe(0);
        sut.GetDomainEvents().OfType<WaitlistCouponExpiredDomainEvent>().Count().ShouldBe(2);
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
        sut.AddEntry(EmailAddress.From("first@example.com"), now, _catalog);
        sut.AddEntry(EmailAddress.From("second@example.com"), now.AddMinutes(1), _catalog);
        sut.ClearDomainEvents();

        // Act
        var result = sut.IssueNextCoupon(CreateTicketedEvent(), _catalog, now);
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
        var result = sut.IssueNextCoupon(CreateTicketedEvent(), _catalog, DateTimeOffset.UtcNow);
        sut.GetDomainEvents().ShouldBeEmpty();
        sut.Coupons.ShouldBeEmpty();
    }

    // Given a waitlist with three active entries
    // When a coupon is issued to the middle entry
    // Then that entry leaves the queue, the others are renumbered, and the coupon goes to that attendee
    [TestMethod]
    public void IssueCouponToEntry_MidQueueEntry_RemovesEntryRenumbersAndReturnsCoupon()
    {
        // Arrange
        var sut = CreateWaitlist();
        var now = DateTimeOffset.UtcNow;
        sut.AddEntry(EmailAddress.From("first@example.com"), now, _catalog);
        sut.AddEntry(EmailAddress.From("second@example.com"), now.AddMinutes(1), _catalog);
        sut.AddEntry(EmailAddress.From("third@example.com"), now.AddMinutes(2), _catalog);
        var target = sut.Entries.Single(e => e.Email.Value == "second@example.com");

        // Act
        var result = sut.IssueCouponToEntry(target.Id, CreateTicketedEvent(), _catalog, now);

        // Assert
        result.Email.Value.ShouldBe("second@example.com");
        result.Source.ShouldBe(CouponSource.Waitlist);
        result.AllowedTicketTypeIds.ShouldBe([DefaultTicketTypeId]);
        target.Status.ShouldBe(WaitlistEntryStatus.Removed);
        sut.GetActivePosition(EmailAddress.From("first@example.com")).ShouldBe(1);
        sut.GetActivePosition(EmailAddress.From("third@example.com")).ShouldBe(2);
        sut.Coupons.ShouldHaveSingleItem().Id.ShouldBe(result.Id);
    }

    // Given a waitlist with an active entry
    // When a coupon is issued to that specific entry
    // Then a WaitlistCouponIssued domain event is raised with the recipient, coupon code, ticket type name, and expiry
    [TestMethod]
    public void IssueCouponToEntry_ActiveEntry_RaisesWaitlistCouponIssuedDomainEvent()
    {
        // Arrange
        var sut = CreateWaitlist();
        var email = EmailAddress.From("vip@example.com");
        sut.AddEntry(EmailAddress.From("first@example.com"), DateTimeOffset.UtcNow, _catalog);
        sut.AddEntry(email, DateTimeOffset.UtcNow, _catalog);
        sut.ClearDomainEvents();
        var catalog = _catalog;
        var entryId = sut.Entries.Single(e => e.Email == email).Id;

        // Act
        var result = sut.IssueCouponToEntry(entryId, CreateTicketedEvent(), catalog, DateTimeOffset.UtcNow);

        // Assert
        sut.GetDomainEvents()
            .OfType<WaitlistCouponIssuedDomainEvent>()
            .ShouldHaveSingleItem()
            .ShouldSatisfyAllConditions(
                e => e.TeamId.ShouldBe(DefaultTeamId),
                e => e.TicketedEventId.ShouldBe(DefaultEventId),
                e => e.TicketTypeId.ShouldBe(DefaultTicketTypeId),
                e => e.RecipientEmail.ShouldBe(email),
                e => e.CouponCode.ShouldBe(result.Code),
                e => e.TicketTypeName.ShouldBe(TicketTypeOf(catalog).Name.Value),
                e => e.ExpiresAt.ShouldBe(result.ExpiresAt));
    }

    // Given the current time falls inside the event's quiet hours
    // When a coupon is issued to a specific entry
    // Then its expiry uses the same claim-window calculation as front-of-queue promotion
    [TestMethod]
    public void IssueCouponToEntry_DuringQuietHours_UsesClaimWindowCalculation()
    {
        // Arrange
        var sut = CreateWaitlist();
        sut.AddEntry(EmailAddress.From("vip@example.com"), DateTimeOffset.UtcNow, _catalog);
        var ticketedEvent = CreateTicketedEvent();
        var catalog = _catalog;
        var now = new DateTimeOffset(2026, 6, 15, 23, 0, 0, TimeSpan.Zero);
        var expected = WaitlistClaimWindowCalculator.ComputeExpiresAt(
            now,
            ticketedEvent.TimeZone,
            ticketedEvent.WaitlistPolicy.QuietHoursStart,
            ticketedEvent.WaitlistPolicy.QuietHoursEnd,
            TicketTypeOf(catalog).ClaimWindowHours);

        // Act
        var result = sut.IssueCouponToEntry(sut.Entries.Single().Id, ticketedEvent, catalog, now);

        // Assert
        result.ExpiresAt.ShouldBe(expected);
    }

    // Given an entry that has already left the waitlist
    // When a coupon is issued to that entry
    // Then it throws the entry-not-active error and issues no coupon
    [TestMethod]
    public void IssueCouponToEntry_RemovedEntry_ThrowsEntryNotActive()
    {
        // Arrange
        var sut = CreateWaitlist();
        var email = EmailAddress.From("vip@example.com");
        sut.AddEntry(email, DateTimeOffset.UtcNow, _catalog);
        var entryId = sut.Entries.Single().Id;
        sut.RemoveEntry(email, _catalog);
        sut.ClearDomainEvents();

        // Act
        var result = ErrorResult.Capture(() =>
            sut.IssueCouponToEntry(entryId, CreateTicketedEvent(), _catalog, DateTimeOffset.UtcNow));

        // Assert
        result.Error.ShouldMatch(Waitlist.Errors.EntryNotActive);
        sut.Coupons.ShouldBeEmpty();
        sut.GetDomainEvents().ShouldBeEmpty();
    }

    // Given a waitlist without the requested entry
    // When a coupon is issued to that entry
    // Then it throws the entry-not-active error
    [TestMethod]
    public void IssueCouponToEntry_UnknownEntry_ThrowsEntryNotActive()
    {
        // Arrange
        var sut = CreateWaitlist();

        // Act
        var result = ErrorResult.Capture(() =>
            sut.IssueCouponToEntry(WaitlistEntryId.New(), CreateTicketedEvent(), _catalog, DateTimeOffset.UtcNow));

        // Assert
        result.Error.ShouldMatch(Waitlist.Errors.EntryNotActive);
    }

    // Given a coupon that has already been redeemed
    // When redemption is attempted again
    // Then it throws a business rule violation instead of silently overwriting
    [TestMethod]
    public void ApplyCouponRedemption_WhenCouponAlreadyRedeemed_ThrowsConflictError()
    {
        // Arrange
        var sut = CreateWaitlist();
        var couponId = IssueCoupon(sut).Id;
        sut.ApplyCouponRedemption(couponId, RedeemerEmail, _catalog);

        // Act & Assert — second redemption attempt must fail, not silently overwrite
        Should.Throw<BusinessRuleViolationException>(() => sut.ApplyCouponRedemption(couponId, RedeemerEmail, _catalog));
    }

    // Given a coupon that was already expired by the expiry job
    // When an attendee then attempts to redeem it
    // Then it throws a business rule violation
    [TestMethod]
    public void ApplyCouponRedemption_WhenCouponAlreadyExpired_ThrowsConflictError()
    {
        // Arrange — simulates the race-loser scenario: expiry job expired first, attendee redeems second
        var sut = CreateWaitlist();
        var coupon = IssueCoupon(sut);
        var couponId = coupon.Id;
        sut.ExpireCoupon(coupon.Id, coupon, _catalog);

        // Act & Assert — the EF Core concurrency token (xmin) is the first guard; this is the
        // fallback guard for in-memory consistency.
        Should.Throw<BusinessRuleViolationException>(() => sut.ApplyCouponRedemption(couponId, RedeemerEmail, _catalog));
    }

    // Given one issued coupon past the cutoff, one issued coupon before it, and one lapsed coupon already redeemed
    // When the lapsed coupons are requested for that cutoff
    // Then only the issued coupon past the cutoff is returned
    [TestMethod]
    public void GetLapsedCouponIds_MixedCoupons_ReturnsOnlyIssuedCouponsPastCutoff()
    {
        // Arrange
        var sut = CreateWaitlist();
        var lapsed = IssueCoupon(sut);
        var redeemed = IssueCoupon(sut);
        sut.ApplyCouponRedemption(redeemed.Id, RedeemerEmail, _catalog);
        var cutoff = lapsed.ExpiresAt;
        sut.AddEntry(EmailAddress.From("late@example.com"), DateTimeOffset.UtcNow, _catalog);
        var notYetLapsed = sut.IssueNextCoupon(CreateTicketedEvent(), _catalog, cutoff)!;

        // Act
        var result = sut.GetLapsedCouponIds(cutoff);

        // Assert
        notYetLapsed.ExpiresAt.ShouldBeGreaterThan(cutoff);
        result.ShouldBe([lapsed.Id]);
    }

    // Given a coupon that has already been expired
    // When expiry is attempted again
    // Then it throws the coupon-not-expirable error
    [TestMethod]
    public void ExpireCoupon_WhenCouponAlreadyExpired_ThrowsCouponNotExpirableError()
    {
        // Arrange
        var sut = CreateWaitlist();
        var coupon = IssueCoupon(sut);
        sut.ExpireCoupon(coupon.Id, coupon, _catalog);

        // Act
        var result = ErrorResult.Capture(() => sut.ExpireCoupon(coupon.Id, coupon, _catalog));

        // Assert
        result.Error.ShouldMatch(WaitlistCoupon.Errors.CouponNotExpirable);
    }

    // Given a coupon that this waitlist never issued
    // When expiry is attempted for it
    // Then it throws the coupon-not-found error
    [TestMethod]
    public void ExpireCoupon_UnknownCoupon_ThrowsCouponNotFoundError()
    {
        // Arrange
        var sut = CreateWaitlist();
        var foreignCoupon = IssueCoupon(CreateWaitlist());

        // Act
        var result = ErrorResult.Capture(() => sut.ExpireCoupon(foreignCoupon.Id, foreignCoupon, _catalog));

        // Assert
        result.Error.ShouldMatch(Waitlist.Errors.CouponNotFound);
    }

    // Given a waitlist where the redeeming email is queued ahead of another attendee
    // When a coupon granting this ticket type is redeemed by that email
    // Then the redeemer's entry is removed, the other attendee moves up, and an entry-removed event is raised
    [TestMethod]
    public void ApplyCouponRedemption_RedeemerHasActiveEntry_RemovesEntryAndRenumbers()
    {
        // Arrange
        var sut = CreateWaitlist();
        var otherEmail = EmailAddress.From("other@example.com");
        sut.AddEntry(RedeemerEmail, DateTimeOffset.UtcNow, _catalog);
        sut.AddEntry(otherEmail, DateTimeOffset.UtcNow, _catalog);

        // Act
        sut.ApplyCouponRedemption(CouponId.New(), RedeemerEmail, _catalog);

        // Assert
        sut.HasActiveEntry(RedeemerEmail).ShouldBeFalse();
        sut.GetActivePosition(otherEmail).ShouldBe(1);
        sut.GetDomainEvents().OfType<WaitlistEntryRemovedDomainEvent>().ShouldHaveSingleItem()
            .Email.ShouldBe(RedeemerEmail);
    }

    // Given a waitlist that did not issue the coupon and has no entry for the redeeming email
    // When a coupon granting this ticket type is redeemed
    // Then the waitlist is left unchanged and raises no events
    [TestMethod]
    public void ApplyCouponRedemption_CouponNotIssuedAndNoEntry_LeavesWaitlistUnchanged()
    {
        // Arrange
        var sut = CreateWaitlist();
        var otherEmail = EmailAddress.From("other@example.com");
        sut.AddEntry(otherEmail, DateTimeOffset.UtcNow, _catalog);
        sut.ClearDomainEvents();

        // Act
        sut.ApplyCouponRedemption(CouponId.New(), RedeemerEmail, _catalog);

        // Assert
        sut.GetActivePosition(otherEmail).ShouldBe(1);
        sut.Coupons.ShouldBeEmpty();
        sut.GetDomainEvents().ShouldBeEmpty();
    }

    // Given a waitlist with three active entries
    // When coupons are issued to every entry (the capacity limit was removed)
    // Then each attendee receives a coupon and offer email in queue order and nobody is left waiting
    [TestMethod]
    public void IssueCouponsToAllEntries_ActiveEntries_IssuesCouponToEveryEntryInQueueOrder()
    {
        // Arrange
        var sut = CreateWaitlist();
        var now = DateTimeOffset.UtcNow;
        sut.AddEntry(EmailAddress.From("first@example.com"), now, _catalog);
        sut.AddEntry(EmailAddress.From("second@example.com"), now.AddMinutes(1), _catalog);
        sut.AddEntry(EmailAddress.From("third@example.com"), now.AddMinutes(2), _catalog);
        sut.ClearDomainEvents();

        // Act
        var catalog = _catalog;
        var coupons = sut.IssueCouponsToAllEntries(CreateTicketedEvent(), catalog, now);

        // Assert
        coupons.Select(c => c.Email.Value).ShouldBe(["first@example.com", "second@example.com", "third@example.com"]);
        sut.ActiveEntryCount.ShouldBe(0);
        sut.Coupons.Count(c => c.Status == WaitlistCouponStatus.Issued).ShouldBe(3);
        TicketTypeOf(catalog).WaitlistHeldCapacity.ShouldBe(3);
        sut.GetDomainEvents().OfType<WaitlistCouponIssuedDomainEvent>().Count().ShouldBe(3);
        sut.GetDomainEvents().OfType<WaitlistExhaustedDomainEvent>().ShouldBeEmpty();
    }

    // Given a waitlist with no active entries
    // When coupons are issued to every entry
    // Then no coupon is issued
    [TestMethod]
    public void IssueCouponsToAllEntries_NoActiveEntries_IssuesNothing()
    {
        // Arrange
        var sut = CreateWaitlist();

        // Act
        var coupons = sut.IssueCouponsToAllEntries(CreateTicketedEvent(), _catalog, DateTimeOffset.UtcNow);

        // Assert
        coupons.ShouldBeEmpty();
        sut.Coupons.ShouldBeEmpty();
        sut.GetDomainEvents().ShouldBeEmpty();
    }

    // Given a waitlist with three active entries and an outstanding coupon
    // When the waitlist is disabled with one freed slot
    // Then the front entry receives a coupon, the rest are removed without an offer, and the outstanding coupon stays issued
    [TestMethod]
    public void Disable_WithFreedSlot_OffersFrontEntryThenRemovesTheRest()
    {
        // Arrange
        var sut = CreateWaitlist();
        var outstanding = IssueCoupon(sut);
        var now = DateTimeOffset.UtcNow;
        sut.AddEntry(EmailAddress.From("first@example.com"), now, _catalog);
        sut.AddEntry(EmailAddress.From("second@example.com"), now.AddMinutes(1), _catalog);
        sut.AddEntry(EmailAddress.From("third@example.com"), now.AddMinutes(2), _catalog);
        sut.ClearDomainEvents();

        // Act
        var coupons = sut.Disable(freedSlots: 1, CreateTicketedEvent(), _catalog, now);

        // Assert
        coupons.ShouldHaveSingleItem().Email.Value.ShouldBe("first@example.com");
        sut.ActiveEntryCount.ShouldBe(0);
        sut.Coupons.Single(c => c.Id == outstanding.Id).Status.ShouldBe(WaitlistCouponStatus.Issued);
        sut.Coupons.Count(c => c.Status == WaitlistCouponStatus.Issued).ShouldBe(2);
        sut.GetDomainEvents().OfType<WaitlistCouponIssuedDomainEvent>()
            .ShouldHaveSingleItem().RecipientEmail.Value.ShouldBe("first@example.com");
        sut.GetDomainEvents().OfType<WaitlistEntryRemovedDomainEvent>()
            .Select(e => e.Email.Value)
            .ShouldBe(["second@example.com", "third@example.com"], ignoreOrder: true);
    }

    // Given a waitlist with two active entries and no outstanding coupons
    // When the waitlist is disabled with no freed slots
    // Then every entry is removed, no coupon is issued, and the waitlist reports it is exhausted
    [TestMethod]
    public void Disable_NoFreedSlotsAndNoOutstandingCoupons_RemovesEveryEntryAndRaisesExhausted()
    {
        // Arrange
        var sut = CreateWaitlist();
        var now = DateTimeOffset.UtcNow;
        sut.AddEntry(EmailAddress.From("first@example.com"), now, _catalog);
        sut.AddEntry(EmailAddress.From("second@example.com"), now.AddMinutes(1), _catalog);
        sut.ClearDomainEvents();

        // Act
        var coupons = sut.Disable(freedSlots: 0, CreateTicketedEvent(), _catalog, now);

        // Assert
        coupons.ShouldBeEmpty();
        sut.Entries.ShouldAllBe(e => e.Status == WaitlistEntryStatus.Removed);
        sut.Coupons.ShouldBeEmpty();
        sut.GetDomainEvents().OfType<WaitlistCouponIssuedDomainEvent>().ShouldBeEmpty();
        sut.GetDomainEvents().OfType<WaitlistExhaustedDomainEvent>().ShouldHaveSingleItem();
    }

    // Given a waitlist with one active entry
    // When the waitlist is disabled with more freed slots than entries
    // Then only that entry receives a coupon
    [TestMethod]
    public void Disable_MoreFreedSlotsThanEntries_IssuesOneCouponPerEntry()
    {
        // Arrange
        var sut = CreateWaitlist();
        sut.AddEntry(EmailAddress.From("first@example.com"), DateTimeOffset.UtcNow, _catalog);

        // Act
        var coupons = sut.Disable(freedSlots: 3, CreateTicketedEvent(), _catalog, DateTimeOffset.UtcNow);

        // Assert
        coupons.ShouldHaveSingleItem();
        sut.ActiveEntryCount.ShouldBe(0);
    }

    // ─── Queued count on the catalog ─────────────────────────────────────────

    private int QueuedCount => TicketTypeOf(_catalog).WaitlistQueuedCount;

    // Given an empty waitlist
    // When two attendees join
    // Then the ticket type counts both as queued
    [TestMethod]
    public void AddEntry_NewEmails_CountsEachJoinOnCatalog()
    {
        // Arrange
        var sut = CreateWaitlist();

        // Act
        sut.AddEntry(EmailAddress.From("alice@example.com"), DateTimeOffset.UtcNow, _catalog);
        sut.AddEntry(EmailAddress.From("bob@example.com"), DateTimeOffset.UtcNow, _catalog);

        // Assert
        QueuedCount.ShouldBe(2);
    }

    // Given an attendee already queued
    // When the same attendee joins again
    // Then they are counted once
    [TestMethod]
    public void AddEntry_EmailAlreadyQueued_DoesNotCountAgain()
    {
        // Arrange
        var sut = CreateWaitlist();
        var email = EmailAddress.From("alice@example.com");
        sut.AddEntry(email, DateTimeOffset.UtcNow, _catalog);

        // Act
        sut.AddEntry(email, DateTimeOffset.UtcNow, _catalog);

        // Assert
        QueuedCount.ShouldBe(1);
    }

    // Given two attendees queued
    // When one of them leaves by email, and an unknown email leaves too
    // Then only the queued attendee's leave is counted
    [TestMethod]
    public void RemoveEntry_ByEmail_CountsLeaveOnlyForQueuedAttendee()
    {
        // Arrange
        var sut = CreateWaitlist();
        var alice = EmailAddress.From("alice@example.com");
        sut.AddEntry(alice, DateTimeOffset.UtcNow, _catalog);
        sut.AddEntry(EmailAddress.From("bob@example.com"), DateTimeOffset.UtcNow, _catalog);

        // Act
        sut.RemoveEntry(alice, _catalog);
        sut.RemoveEntry(EmailAddress.From("nobody@example.com"), _catalog);

        // Assert
        QueuedCount.ShouldBe(1);
    }

    // Given two attendees queued
    // When an organizer removes one entry, twice
    // Then the leave is counted once
    [TestMethod]
    public void RemoveEntry_ByIdTwice_CountsLeaveOnce()
    {
        // Arrange
        var sut = CreateWaitlist();
        sut.AddEntry(EmailAddress.From("alice@example.com"), DateTimeOffset.UtcNow, _catalog);
        sut.AddEntry(EmailAddress.From("bob@example.com"), DateTimeOffset.UtcNow, _catalog);
        var entryId = sut.Entries[0].Id;

        // Act
        sut.RemoveEntry(entryId, _catalog);
        sut.RemoveEntry(entryId, _catalog);

        // Assert
        QueuedCount.ShouldBe(1);
    }

    // Given two attendees queued
    // When the next coupon is issued
    // Then the attendee at the front moves from the queued count to a held seat
    [TestMethod]
    public void IssueNextCoupon_QueuedAttendee_MovesFromQueuedCountToHold()
    {
        // Arrange
        var sut = CreateWaitlist();
        sut.AddEntry(EmailAddress.From("alice@example.com"), DateTimeOffset.UtcNow, _catalog);
        sut.AddEntry(EmailAddress.From("bob@example.com"), DateTimeOffset.UtcNow, _catalog);

        // Act
        sut.IssueNextCoupon(CreateTicketedEvent(), _catalog, DateTimeOffset.UtcNow);

        // Assert
        QueuedCount.ShouldBe(1);
        TicketTypeOf(_catalog).WaitlistHeldCapacity.ShouldBe(1);
    }

    // Given two attendees queued
    // When an organizer promotes the second one as a VIP
    // Then that attendee moves from the queued count to a held seat
    [TestMethod]
    public void IssueCouponToEntry_QueuedAttendee_MovesFromQueuedCountToHold()
    {
        // Arrange
        var sut = CreateWaitlist();
        sut.AddEntry(EmailAddress.From("alice@example.com"), DateTimeOffset.UtcNow, _catalog);
        sut.AddEntry(EmailAddress.From("vip@example.com"), DateTimeOffset.UtcNow, _catalog);

        // Act
        sut.IssueCouponToEntry(sut.Entries[1].Id, CreateTicketedEvent(), _catalog, DateTimeOffset.UtcNow);

        // Assert
        QueuedCount.ShouldBe(1);
        TicketTypeOf(_catalog).WaitlistHeldCapacity.ShouldBe(1);
    }

    // Given the redeeming attendee is queued alongside someone else
    // When they redeem a coupon for this ticket type
    // Then their leave is counted
    [TestMethod]
    public void ApplyCouponRedemption_RedeemerQueued_CountsLeave()
    {
        // Arrange
        var sut = CreateWaitlist();
        sut.AddEntry(RedeemerEmail, DateTimeOffset.UtcNow, _catalog);
        sut.AddEntry(EmailAddress.From("other@example.com"), DateTimeOffset.UtcNow, _catalog);

        // Act
        sut.ApplyCouponRedemption(CouponId.New(), RedeemerEmail, _catalog);

        // Assert
        QueuedCount.ShouldBe(1);
    }

    // Given three attendees queued
    // When the waitlist is disabled with one seat free
    // Then nobody is counted as queued any more
    [TestMethod]
    public void Disable_QueuedAttendees_ClearsQueuedCount()
    {
        // Arrange
        var sut = CreateWaitlist();
        sut.AddEntry(EmailAddress.From("first@example.com"), DateTimeOffset.UtcNow, _catalog);
        sut.AddEntry(EmailAddress.From("second@example.com"), DateTimeOffset.UtcNow, _catalog);
        sut.AddEntry(EmailAddress.From("third@example.com"), DateTimeOffset.UtcNow, _catalog);

        // Act
        sut.Disable(freedSlots: 1, CreateTicketedEvent(), _catalog, DateTimeOffset.UtcNow);

        // Assert
        QueuedCount.ShouldBe(0);
    }

    // Given three attendees queued
    // When everyone is issued a coupon
    // Then nobody is counted as queued any more
    [TestMethod]
    public void IssueCouponsToAllEntries_QueuedAttendees_ClearsQueuedCount()
    {
        // Arrange
        var sut = CreateWaitlist();
        sut.AddEntry(EmailAddress.From("first@example.com"), DateTimeOffset.UtcNow, _catalog);
        sut.AddEntry(EmailAddress.From("second@example.com"), DateTimeOffset.UtcNow, _catalog);
        sut.AddEntry(EmailAddress.From("third@example.com"), DateTimeOffset.UtcNow, _catalog);

        // Act
        sut.IssueCouponsToAllEntries(CreateTicketedEvent(), _catalog, DateTimeOffset.UtcNow);

        // Assert
        QueuedCount.ShouldBe(0);
    }
}
