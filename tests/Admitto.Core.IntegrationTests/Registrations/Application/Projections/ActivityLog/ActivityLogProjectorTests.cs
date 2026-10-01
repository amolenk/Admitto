using System.Text.Json;
using Amolenk.Admitto.Core.Registrations.Application.Projections.ActivityLog;
using Amolenk.Admitto.Core.Registrations.Contracts.ValueObjects;
using Amolenk.Admitto.Core.Registrations.Domain.DomainEvents;
using Amolenk.Admitto.Core.Registrations.Domain.Entities;
using Amolenk.Admitto.Core.Registrations.Domain.ValueObjects;
using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace Amolenk.Admitto.Core.IntegrationTests.Registrations.Application.Projections.ActivityLog;

[TestClass]
public sealed class ActivityLogProjectorTests(TestContext testContext) : AspireIntegrationTestBase
{
    // Given an AttendeeRegistered domain event
    // When the projector handles the event
    // Then a Registered activity log entry is created with the event's occurred-on timestamp and no metadata
    [TestMethod]
    public async ValueTask HandleAsync_AttendeeRegistered_CreatesRegisteredEntry()
    {
        var registrationId = RegistrationId.New();
        var teamId = TeamId.New();
        var eventId = TicketedEventId.New();
        var occurredOn = DateTimeOffset.UtcNow.AddMinutes(-10);
        var domainEvent = new AttendeeRegisteredDomainEvent(
            teamId,
            eventId,
            registrationId,
            EmailAddress.From("alice@example.com"),
            FirstName.From("Alice"),
            LastName.From("Doe"),
            [],
            [],
            occurredOn) with { OccurredOn = occurredOn };

        var projector = new ActivityLogProjector(Environment.RegistrationsDatabase.Context, Environment.RegistrationsDatabase.Context);
        await projector.HandleAsync(domainEvent, testContext.CancellationToken);

        await Environment.RegistrationsDatabase.AssertAsync(async db =>
        {
            var entry = await db.ActivityLog
                .SingleOrDefaultAsync(
                    a => a.RegistrationId == registrationId.Value,
                    testContext.CancellationToken);
            entry.ShouldNotBeNull();
            entry.ActivityType.ShouldBe(ActivityType.Registered);
            entry.OccurredAt.ShouldBe(occurredOn);
            entry.Metadata.ShouldBeNull();
        });
    }

    // Given a RegistrationReconfirmed domain event
    // When the projector handles the event
    // Then a Reconfirmed activity log entry is created with the reconfirmed-at timestamp and no metadata
    [TestMethod]
    public async ValueTask HandleAsync_RegistrationReconfirmed_CreatesReconfirmedEntry()
    {
        var registrationId = RegistrationId.New();
        var teamId = TeamId.New();
        var eventId = TicketedEventId.New();
        var reconfirmedAt = DateTimeOffset.UtcNow.AddMinutes(-5);
        var domainEvent = new RegistrationReconfirmedDomainEvent(
            teamId,
            eventId,
            registrationId,
            EmailAddress.From("alice@example.com"),
            reconfirmedAt);

        var projector = new ActivityLogProjector(Environment.RegistrationsDatabase.Context, Environment.RegistrationsDatabase.Context);
        await projector.HandleAsync(domainEvent, testContext.CancellationToken);

        await Environment.RegistrationsDatabase.AssertAsync(async db =>
        {
            var entry = await db.ActivityLog
                .SingleOrDefaultAsync(
                    a => a.RegistrationId == registrationId.Value,
                    testContext.CancellationToken);
            entry.ShouldNotBeNull();
            entry.ActivityType.ShouldBe(ActivityType.Reconfirmed);
            entry.OccurredAt.ShouldBe(reconfirmedAt);
            entry.Metadata.ShouldBeNull();
        });
    }

    // Given a RegistrationCancelled domain event with a cancellation reason
    // When the projector handles the event
    // Then a Cancelled activity log entry is created with the reason stored as metadata
    [TestMethod]
    public async ValueTask HandleAsync_RegistrationCancelled_CreatesCancelledEntryWithReason()
    {
        var registrationId = RegistrationId.New();
        var teamId = TeamId.New();
        var eventId = TicketedEventId.New();
        var occurredOn = DateTimeOffset.UtcNow.AddMinutes(-3);
        var domainEvent = new RegistrationCancelledDomainEvent(
            teamId,
            eventId,
            registrationId,
            EmailAddress.From("alice@example.com"),
            FirstName.From("Alice"),
            LastName.From("Anderson"),
            CancellationReason.VisaLetterDenied,
            WasWaitlisted: false) with { OccurredOn = occurredOn };

        var projector = new ActivityLogProjector(Environment.RegistrationsDatabase.Context, Environment.RegistrationsDatabase.Context);
        await projector.HandleAsync(domainEvent, testContext.CancellationToken);

        await Environment.RegistrationsDatabase.AssertAsync(async db =>
        {
            var entry = await db.ActivityLog
                .SingleOrDefaultAsync(
                    a => a.RegistrationId == registrationId.Value,
                    testContext.CancellationToken);
            entry.ShouldNotBeNull();
            entry.ActivityType.ShouldBe(ActivityType.Cancelled);
            entry.OccurredAt.ShouldBe(occurredOn);
            entry.Metadata.ShouldBe("VisaLetterDenied");
        });
    }

    // Given a registration's selected tickets change from one type to another
    // When the change is recorded
    // Then an activity log entry is created with metadata listing the old and new ticket type names
    [TestMethod]
    public async ValueTask HandleAsync_TicketsChanged_CreatesTicketsChangedEntryWithMetadata()
    {
        var registrationId = RegistrationId.New();
        var teamId = TeamId.New();
        var eventId = TicketedEventId.New();
        var changedAt = DateTimeOffset.UtcNow;
        var earlyBirdId = TicketTypeId.New();
        var workshopId = TicketTypeId.New();
        var domainEvent = new TicketsChangedDomainEvent(
            teamId,
            eventId,
            registrationId,
            EmailAddress.From("alice@example.com"),
            FirstName.From("Alice"),
            LastName.From("Doe"),
            OldTickets: [new TicketTypeSnapshot(earlyBirdId, TicketTypeName.From("Early Bird"), [])],
            NewTickets: [new TicketTypeSnapshot(workshopId, TicketTypeName.From("Workshop"), [])],
            OldWaitlistedTickets: [],
            NewWaitlistedTickets: [],
            ChangedAt: changedAt);

        var projector = new ActivityLogProjector(Environment.RegistrationsDatabase.Context, Environment.RegistrationsDatabase.Context);
        await projector.HandleAsync(domainEvent, testContext.CancellationToken);

        await Environment.RegistrationsDatabase.AssertAsync(async db =>
        {
            var entry = await db.ActivityLog
                .SingleOrDefaultAsync(
                    a => a.RegistrationId == registrationId.Value,
                    testContext.CancellationToken);
            entry.ShouldNotBeNull();
            entry.ActivityType.ShouldBe(ActivityType.TicketsChanged);
            entry.OccurredAt.ShouldBe(changedAt);

            using var doc = JsonDocument.Parse(entry.Metadata!);
            var from = doc.RootElement.GetProperty("from").EnumerateArray().Select(e => e.GetString()).ToArray();
            var to = doc.RootElement.GetProperty("to").EnumerateArray().Select(e => e.GetString()).ToArray();
            from.ShouldBe(["Early Bird"]);
            to.ShouldBe(["Workshop"]);
        });
    }

    // Given a registration's confirmed tickets stay the same while it moves from one waitlist to another
    // When the change is recorded
    // Then no tickets-changed entry is created, but a waitlist-selection-changed entry records the move
    [TestMethod]
    public async ValueTask HandleAsync_TicketsChangedWaitlistOnly_CreatesWaitlistSelectionChangedEntryOnly()
    {
        var registrationId = RegistrationId.New();
        var earlyBird = new TicketTypeSnapshot(TicketTypeId.New(), TicketTypeName.From("Early Bird"), []);
        var changedAt = DateTimeOffset.UtcNow;
        var domainEvent = new TicketsChangedDomainEvent(
            TeamId.New(),
            TicketedEventId.New(),
            registrationId,
            EmailAddress.From("alice@example.com"),
            FirstName.From("Alice"),
            LastName.From("Doe"),
            OldTickets: [earlyBird],
            NewTickets: [earlyBird],
            OldWaitlistedTickets: [new TicketTypeSnapshot(TicketTypeId.New(), TicketTypeName.From("Workshop A"), [])],
            NewWaitlistedTickets: [new TicketTypeSnapshot(TicketTypeId.New(), TicketTypeName.From("Workshop B"), [])],
            ChangedAt: changedAt);

        var projector = new ActivityLogProjector(Environment.RegistrationsDatabase.Context, Environment.RegistrationsDatabase.Context);
        await projector.HandleAsync(domainEvent, testContext.CancellationToken);

        await Environment.RegistrationsDatabase.AssertAsync(async db =>
        {
            var entry = await db.ActivityLog.SingleAsync(
                a => a.RegistrationId == registrationId.Value,
                testContext.CancellationToken);
            entry.ActivityType.ShouldBe(ActivityType.WaitlistSelectionChanged);
            entry.OccurredAt.ShouldBe(changedAt);

            using var doc = JsonDocument.Parse(entry.Metadata!);
            var from = doc.RootElement.GetProperty("from").EnumerateArray().Select(e => e.GetString()).ToArray();
            var to = doc.RootElement.GetProperty("to").EnumerateArray().Select(e => e.GetString()).ToArray();
            from.ShouldBe(["Workshop A"]);
            to.ShouldBe(["Workshop B"]);
        });
    }

    // Given a registration whose confirmed tickets and waitlisted tickets both change
    // When the change is recorded
    // Then both a tickets-changed entry and a waitlist-selection-changed entry are created
    [TestMethod]
    public async ValueTask HandleAsync_TicketsChangedBothConfirmedAndWaitlisted_CreatesBothEntries()
    {
        var registrationId = RegistrationId.New();
        var changedAt = DateTimeOffset.UtcNow;
        var domainEvent = new TicketsChangedDomainEvent(
            TeamId.New(),
            TicketedEventId.New(),
            registrationId,
            EmailAddress.From("alice@example.com"),
            FirstName.From("Alice"),
            LastName.From("Doe"),
            OldTickets: [new TicketTypeSnapshot(TicketTypeId.New(), TicketTypeName.From("Early Bird"), [])],
            NewTickets: [new TicketTypeSnapshot(TicketTypeId.New(), TicketTypeName.From("General"), [])],
            OldWaitlistedTickets: [],
            NewWaitlistedTickets: [new TicketTypeSnapshot(TicketTypeId.New(), TicketTypeName.From("Workshop A"), [])],
            ChangedAt: changedAt);

        var projector = new ActivityLogProjector(Environment.RegistrationsDatabase.Context, Environment.RegistrationsDatabase.Context);
        await projector.HandleAsync(domainEvent, testContext.CancellationToken);

        await Environment.RegistrationsDatabase.AssertAsync(async db =>
        {
            var entries = await db.ActivityLog
                .Where(a => a.RegistrationId == registrationId.Value)
                .ToListAsync(testContext.CancellationToken);
            entries.Count.ShouldBe(2);
            entries.ShouldContain(a => a.ActivityType == ActivityType.TicketsChanged);
            entries.ShouldContain(a => a.ActivityType == ActivityType.WaitlistSelectionChanged);
        });
    }

    // Given a registration whose confirmed and waitlisted ticket selections are both unchanged
    // When the event is handled
    // Then no activity log entry is created
    [TestMethod]
    public async ValueTask HandleAsync_TicketsChangedNothingChanged_CreatesNoEntry()
    {
        var registrationId = RegistrationId.New();
        var earlyBird = new TicketTypeSnapshot(TicketTypeId.New(), TicketTypeName.From("Early Bird"), []);
        var domainEvent = new TicketsChangedDomainEvent(
            TeamId.New(),
            TicketedEventId.New(),
            registrationId,
            EmailAddress.From("alice@example.com"),
            FirstName.From("Alice"),
            LastName.From("Doe"),
            OldTickets: [earlyBird],
            NewTickets: [earlyBird],
            OldWaitlistedTickets: [],
            NewWaitlistedTickets: [],
            ChangedAt: DateTimeOffset.UtcNow);

        var projector = new ActivityLogProjector(Environment.RegistrationsDatabase.Context, Environment.RegistrationsDatabase.Context);
        await projector.HandleAsync(domainEvent, testContext.CancellationToken);

        await Environment.RegistrationsDatabase.AssertAsync(async db =>
        {
            (await db.ActivityLog.CountAsync(a => a.RegistrationId == registrationId.Value, testContext.CancellationToken))
                .ShouldBe(0);
        });
    }

    // Given a RegistrationCheckedIn domain event
    // When the projector handles the event
    // Then a CheckedIn activity log entry is created with no metadata
    [TestMethod]
    public async ValueTask HandleAsync_RegistrationCheckedIn_CreatesCheckedInEntryWithoutMetadata()
    {
        var registrationId = RegistrationId.New();
        var teamId = TeamId.New();
        var eventId = TicketedEventId.New();
        var checkedInAt = DateTimeOffset.UtcNow.AddMinutes(-2);
        var domainEvent = new RegistrationCheckedInDomainEvent(
            teamId,
            eventId,
            registrationId,
            checkedInAt);

        var projector = new ActivityLogProjector(Environment.RegistrationsDatabase.Context, Environment.RegistrationsDatabase.Context);
        await projector.HandleAsync(domainEvent, testContext.CancellationToken);

        await Environment.RegistrationsDatabase.AssertAsync(async db =>
        {
            var entry = await db.ActivityLog.SingleAsync(
                a => a.RegistrationId == registrationId.Value,
                testContext.CancellationToken);
            entry.ActivityType.ShouldBe(ActivityType.CheckedIn);
            entry.OccurredAt.ShouldBe(checkedInAt);
            entry.Metadata.ShouldBeNull();
        });
    }

    // Given a RegistrationCheckedIn domain event from the shared scanner
    // When the projector handles the event
    // Then the CheckedIn activity log entry records the shared-scanner source
    [TestMethod]
    public async ValueTask HandleAsync_RegistrationCheckedInFromSharedScanner_CreatesCheckedInEntryWithSourceMetadata()
    {
        var registrationId = RegistrationId.New();
        var teamId = TeamId.New();
        var eventId = TicketedEventId.New();
        var checkedInAt = DateTimeOffset.UtcNow.AddMinutes(-2);
        var domainEvent = new RegistrationCheckedInDomainEvent(
            teamId,
            eventId,
            registrationId,
            checkedInAt,
            CheckInSource.SharedScanner);

        var projector = new ActivityLogProjector(Environment.RegistrationsDatabase.Context, Environment.RegistrationsDatabase.Context);
        await projector.HandleAsync(domainEvent, testContext.CancellationToken);

        await Environment.RegistrationsDatabase.AssertAsync(async db =>
        {
            var entry = await db.ActivityLog.SingleAsync(
                a => a.RegistrationId == registrationId.Value,
                testContext.CancellationToken);
            entry.ActivityType.ShouldBe(ActivityType.CheckedIn);
            entry.OccurredAt.ShouldBe(checkedInAt);

            using var doc = JsonDocument.Parse(entry.Metadata!);
            doc.RootElement.GetProperty("source").GetString().ShouldBe("SharedScanner");
        });
    }

    // Given a registration
    // When multiple domain events for that registration are handled in sequence
    // Then an activity log entry accumulates for each event
    [TestMethod]
    public async ValueTask HandleAsync_MultipleEventsForSameRegistration_AllEntriesAccumulate()
    {
        var registrationId = RegistrationId.New();
        var teamId = TeamId.New();
        var eventId = TicketedEventId.New();
        var now = DateTimeOffset.UtcNow;
        var projector = new ActivityLogProjector(Environment.RegistrationsDatabase.Context, Environment.RegistrationsDatabase.Context);

        await projector.HandleAsync(
            new AttendeeRegisteredDomainEvent(
                teamId,
                eventId,
                registrationId,
                EmailAddress.From("alice@example.com"),
                FirstName.From("Alice"),
                LastName.From("Doe"),
                [],
                [],
                now.AddMinutes(-10)) with { OccurredOn = now.AddMinutes(-10) },
            testContext.CancellationToken);
        await projector.HandleAsync(
            new RegistrationReconfirmedDomainEvent(
                teamId,
                eventId,
                registrationId,
                EmailAddress.From("alice@example.com"),
                now.AddMinutes(-1)),
            testContext.CancellationToken);

        await Environment.RegistrationsDatabase.AssertAsync(async db =>
        {
            var entries = await db.ActivityLog
                .Where(a => a.RegistrationId == registrationId.Value)
                .ToListAsync(testContext.CancellationToken);
            entries.Count.ShouldBe(2);
            entries.ShouldContain(a => a.ActivityType == ActivityType.Registered);
            entries.ShouldContain(a => a.ActivityType == ActivityType.Reconfirmed);
        });
    }

    // Given a WaitlistCouponIssued domain event that already carries the recipient's registration id
    // When the projector handles the event
    // Then a WaitlistOfferSent activity log entry is created with the ticket type, expiry and reason as metadata
    [TestMethod]
    public async ValueTask HandleAsync_WaitlistCouponIssuedWithRegistrationId_CreatesWaitlistOfferSentEntry()
    {
        var registrationId = RegistrationId.New();
        var teamId = TeamId.New();
        var eventId = TicketedEventId.New();
        var expiresAt = DateTimeOffset.UtcNow.AddHours(24);
        var domainEvent = new WaitlistCouponIssuedDomainEvent(
            teamId,
            eventId,
            TicketTypeId.New(),
            EmailAddress.From("alice@example.com"),
            CouponCode.New(),
            "Workshop",
            expiresAt,
            WaitlistOfferReason.AutomaticPromotion,
            registrationId);

        var projector = new ActivityLogProjector(Environment.RegistrationsDatabase.Context, Environment.RegistrationsDatabase.Context);
        await projector.HandleAsync(domainEvent, testContext.CancellationToken);

        await Environment.RegistrationsDatabase.AssertAsync(async db =>
        {
            var entry = await db.ActivityLog.SingleAsync(
                a => a.RegistrationId == registrationId.Value,
                testContext.CancellationToken);
            entry.ActivityType.ShouldBe(ActivityType.WaitlistOfferSent);
            entry.OccurredAt.ShouldBe(domainEvent.OccurredOn);

            using var doc = JsonDocument.Parse(entry.Metadata!);
            doc.RootElement.GetProperty("ticketType").GetString().ShouldBe("Workshop");
            doc.RootElement.GetProperty("reason").GetString().ShouldBe(nameof(WaitlistOfferReason.AutomaticPromotion));
        });
    }

    // Given a WaitlistCouponIssued domain event with no registration id, for a recipient with a matching registration
    // When the projector handles the event
    // Then the registration is resolved by team, event and email, and a WaitlistOfferSent entry is created for it
    [TestMethod]
    public async ValueTask HandleAsync_WaitlistCouponIssuedWithoutRegistrationId_ResolvesRegistrationByEmail()
    {
        var teamId = TeamId.New();
        var eventId = TicketedEventId.New();
        var email = EmailAddress.From("alice@example.com");
        var registration = Registration.Create(
            teamId, eventId, email, FirstName.From("Alice"), LastName.From("Doe"), []);
        await Environment.RegistrationsDatabase.SeedAsync(db => db.Registrations.Add(registration));

        var domainEvent = new WaitlistCouponIssuedDomainEvent(
            teamId,
            eventId,
            TicketTypeId.New(),
            email,
            CouponCode.New(),
            "Workshop",
            DateTimeOffset.UtcNow.AddHours(24),
            WaitlistOfferReason.VipPromotion,
            RegistrationId: null);

        var projector = new ActivityLogProjector(Environment.RegistrationsDatabase.Context, Environment.RegistrationsDatabase.Context);
        await projector.HandleAsync(domainEvent, testContext.CancellationToken);

        await Environment.RegistrationsDatabase.AssertAsync(async db =>
        {
            var entry = await db.ActivityLog.SingleAsync(
                a => a.RegistrationId == registration.Id.Value,
                testContext.CancellationToken);
            entry.ActivityType.ShouldBe(ActivityType.WaitlistOfferSent);
        });
    }

    // Given a WaitlistCouponIssued domain event with no registration id and no matching registration
    // When the projector handles the event
    // Then no activity log entry is created
    [TestMethod]
    public async ValueTask HandleAsync_WaitlistCouponIssuedWithoutMatchingRegistration_CreatesNoEntry()
    {
        var domainEvent = new WaitlistCouponIssuedDomainEvent(
            TeamId.New(),
            TicketedEventId.New(),
            TicketTypeId.New(),
            EmailAddress.From("nobody@example.com"),
            CouponCode.New(),
            "Workshop",
            DateTimeOffset.UtcNow.AddHours(24),
            WaitlistOfferReason.AutomaticPromotion,
            RegistrationId: null);

        var projector = new ActivityLogProjector(Environment.RegistrationsDatabase.Context, Environment.RegistrationsDatabase.Context);
        await projector.HandleAsync(domainEvent, testContext.CancellationToken);

        await Environment.RegistrationsDatabase.AssertAsync(async db =>
        {
            (await db.ActivityLog.CountAsync(testContext.CancellationToken)).ShouldBe(0);
        });
    }

    // Given a WaitlistCouponExpired domain event for a recipient with a matching registration
    // When the projector handles the event
    // Then a WaitlistOfferExpired activity log entry is created with the ticket type as metadata
    [TestMethod]
    public async ValueTask HandleAsync_WaitlistCouponExpired_CreatesWaitlistOfferExpiredEntry()
    {
        var teamId = TeamId.New();
        var eventId = TicketedEventId.New();
        var email = EmailAddress.From("alice@example.com");
        var registration = Registration.Create(
            teamId, eventId, email, FirstName.From("Alice"), LastName.From("Doe"), []);
        await Environment.RegistrationsDatabase.SeedAsync(db => db.Registrations.Add(registration));

        var domainEvent = new WaitlistCouponExpiredDomainEvent(
            teamId,
            eventId,
            TicketTypeId.New(),
            email,
            CouponCode.New(),
            "Workshop",
            RegistrationClosed: false);

        var projector = new ActivityLogProjector(Environment.RegistrationsDatabase.Context, Environment.RegistrationsDatabase.Context);
        await projector.HandleAsync(domainEvent, testContext.CancellationToken);

        await Environment.RegistrationsDatabase.AssertAsync(async db =>
        {
            var entry = await db.ActivityLog.SingleAsync(
                a => a.RegistrationId == registration.Id.Value,
                testContext.CancellationToken);
            entry.ActivityType.ShouldBe(ActivityType.WaitlistOfferExpired);

            using var doc = JsonDocument.Parse(entry.Metadata!);
            doc.RootElement.GetProperty("ticketType").GetString().ShouldBe("Workshop");
        });
    }

    // Given a WaitlistEntryRemoved domain event for an email with a matching registration
    // When the projector handles the event
    // Then a WaitlistRemoved activity log entry is created with no metadata
    [TestMethod]
    public async ValueTask HandleAsync_WaitlistEntryRemoved_CreatesWaitlistRemovedEntry()
    {
        var teamId = TeamId.New();
        var eventId = TicketedEventId.New();
        var email = EmailAddress.From("alice@example.com");
        var registration = Registration.Create(
            teamId, eventId, email, FirstName.From("Alice"), LastName.From("Doe"), []);
        await Environment.RegistrationsDatabase.SeedAsync(db => db.Registrations.Add(registration));

        var domainEvent = new WaitlistEntryRemovedDomainEvent(
            teamId,
            eventId,
            TicketTypeId.New(),
            WaitlistEntryId.New(),
            email);

        var projector = new ActivityLogProjector(Environment.RegistrationsDatabase.Context, Environment.RegistrationsDatabase.Context);
        await projector.HandleAsync(domainEvent, testContext.CancellationToken);

        await Environment.RegistrationsDatabase.AssertAsync(async db =>
        {
            var entry = await db.ActivityLog.SingleAsync(
                a => a.RegistrationId == registration.Id.Value,
                testContext.CancellationToken);
            entry.ActivityType.ShouldBe(ActivityType.WaitlistRemoved);
            entry.Metadata.ShouldBeNull();
        });
    }

    // Given a registration that was just added to the same unit of work (not yet persisted), e.g. because
    // redeeming a waitlist coupon creates it in the same command as the waitlist entry removal
    // When the projector handles a WaitlistEntryRemoved event for that email
    // Then the registration is resolved from the change tracker and a WaitlistRemoved entry is created for it
    [TestMethod]
    public async ValueTask HandleAsync_WaitlistEntryRemovedForRegistrationAddedInSameUnitOfWork_ResolvesFromChangeTracker()
    {
        var teamId = TeamId.New();
        var eventId = TicketedEventId.New();
        var email = EmailAddress.From("alice@example.com");
        var registration = Registration.Create(
            teamId, eventId, email, FirstName.From("Alice"), LastName.From("Doe"), []);

        // Added to the same context but not saved yet, mirroring a handler that creates the registration
        // in the same unit of work as the waitlist cleanup that raises this event.
        await Environment.RegistrationsDatabase.Context.Registrations.AddAsync(registration, testContext.CancellationToken);

        var domainEvent = new WaitlistEntryRemovedDomainEvent(
            teamId,
            eventId,
            TicketTypeId.New(),
            WaitlistEntryId.New(),
            email);

        var projector = new ActivityLogProjector(Environment.RegistrationsDatabase.Context, Environment.RegistrationsDatabase.Context);
        await projector.HandleAsync(domainEvent, testContext.CancellationToken);

        await Environment.RegistrationsDatabase.AssertAsync(async db =>
        {
            var entry = await db.ActivityLog.SingleAsync(
                a => a.RegistrationId == registration.Id.Value,
                testContext.CancellationToken);
            entry.ActivityType.ShouldBe(ActivityType.WaitlistRemoved);
        });
    }

    // Given a WaitlistEntryRemoved domain event for an email with no matching registration
    // When the projector handles the event
    // Then no activity log entry is created
    [TestMethod]
    public async ValueTask HandleAsync_WaitlistEntryRemovedWithoutMatchingRegistration_CreatesNoEntry()
    {
        var domainEvent = new WaitlistEntryRemovedDomainEvent(
            TeamId.New(),
            TicketedEventId.New(),
            TicketTypeId.New(),
            WaitlistEntryId.New(),
            EmailAddress.From("nobody@example.com"));

        var projector = new ActivityLogProjector(Environment.RegistrationsDatabase.Context, Environment.RegistrationsDatabase.Context);
        await projector.HandleAsync(domainEvent, testContext.CancellationToken);

        await Environment.RegistrationsDatabase.AssertAsync(async db =>
        {
            (await db.ActivityLog.CountAsync(testContext.CancellationToken)).ShouldBe(0);
        });
    }
}
