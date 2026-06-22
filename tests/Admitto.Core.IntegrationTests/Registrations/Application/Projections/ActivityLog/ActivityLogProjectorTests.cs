using System.Text.Json;
using Amolenk.Admitto.Core.Registrations.Application.Projections.ActivityLog;
using Amolenk.Admitto.Core.Registrations.Contracts.ValueObjects;
using Amolenk.Admitto.Core.Registrations.Domain.DomainEvents;
using Amolenk.Admitto.Core.Registrations.Domain.ValueObjects;
using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace Amolenk.Admitto.Core.IntegrationTests.Registrations.Application.Projections.ActivityLog;

[TestClass]
public sealed class ActivityLogProjectorTests(TestContext testContext) : AspireIntegrationTestBase
{
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
            occurredOn) with { OccurredOn = occurredOn };

        var projector = new ActivityLogProjector(Environment.RegistrationsDatabase.Context);
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

        var projector = new ActivityLogProjector(Environment.RegistrationsDatabase.Context);
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
            CancellationReason.VisaLetterDenied) with { OccurredOn = occurredOn };

        var projector = new ActivityLogProjector(Environment.RegistrationsDatabase.Context);
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
            ChangedAt: changedAt);

        var projector = new ActivityLogProjector(Environment.RegistrationsDatabase.Context);
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
            var from = doc.RootElement.GetProperty("from").EnumerateArray().Select(e => e.GetGuid()).ToArray();
            var to = doc.RootElement.GetProperty("to").EnumerateArray().Select(e => e.GetGuid()).ToArray();
            from.ShouldBe([earlyBirdId.Value]);
            to.ShouldBe([workshopId.Value]);
        });
    }

    [TestMethod]
    public async ValueTask HandleAsync_MultipleEventsForSameRegistration_AllEntriesAccumulate()
    {
        var registrationId = RegistrationId.New();
        var teamId = TeamId.New();
        var eventId = TicketedEventId.New();
        var now = DateTimeOffset.UtcNow;
        var projector = new ActivityLogProjector(Environment.RegistrationsDatabase.Context);

        await projector.HandleAsync(
            new AttendeeRegisteredDomainEvent(
                teamId,
                eventId,
                registrationId,
                EmailAddress.From("alice@example.com"),
                FirstName.From("Alice"),
                LastName.From("Doe"),
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
}
