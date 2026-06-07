using Amolenk.Admitto.Core.Registrations.Application.UseCases.Registrations.WriteActivityLog;
using Amolenk.Admitto.Core.Registrations.Domain.ValueObjects;
using Amolenk.Admitto.Core.Registrations.Contracts.ValueObjects;
using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace Amolenk.Admitto.Core.IntegrationTests.Registrations.Application.UseCases.Registrations.WriteActivityLog;

[TestClass]
public sealed class WriteActivityLogHandlerTests(TestContext testContext) : AspireIntegrationTestBase
{
    [TestMethod]
    public async ValueTask AttendeeRegistered_CreatesRegisteredEntry()
    {
        var registrationId = RegistrationId.New();
        var teamId = TeamId.New();
        var eventId = TicketedEventId.New();
        var occurredOn = DateTimeOffset.UtcNow.AddMinutes(-10);

        var handler = new WriteActivityLogHandler(Environment.RegistrationsDatabase.Context);
        await handler.HandleAsync(
            new WriteActivityLogCommand(teamId, eventId, registrationId, ActivityType.Registered, occurredOn),
            testContext.CancellationToken);

        await Environment.RegistrationsDatabase.AssertAsync(async db =>
        {
            var entry = await db.ActivityLog
                .SingleOrDefaultAsync(
                    a => a.RegistrationId == registrationId,
                    testContext.CancellationToken);
            entry.ShouldNotBeNull();
            entry.ActivityType.ShouldBe(ActivityType.Registered);
            entry.OccurredAt.ShouldBe(occurredOn);
            entry.Metadata.ShouldBeNull();
        });
    }

    [TestMethod]
    public async ValueTask RegistrationReconfirmed_CreatesReconfirmedEntry()
    {
        var registrationId = RegistrationId.New();
        var teamId = TeamId.New();
        var eventId = TicketedEventId.New();
        var reconfirmedAt = DateTimeOffset.UtcNow.AddMinutes(-5);

        var handler = new WriteActivityLogHandler(Environment.RegistrationsDatabase.Context);
        await handler.HandleAsync(
            new WriteActivityLogCommand(teamId, eventId, registrationId, ActivityType.Reconfirmed, reconfirmedAt),
            testContext.CancellationToken);

        await Environment.RegistrationsDatabase.AssertAsync(async db =>
        {
            var entry = await db.ActivityLog
                .SingleOrDefaultAsync(
                    a => a.RegistrationId == registrationId,
                    testContext.CancellationToken);
            entry.ShouldNotBeNull();
            entry.ActivityType.ShouldBe(ActivityType.Reconfirmed);
            entry.OccurredAt.ShouldBe(reconfirmedAt);
            entry.Metadata.ShouldBeNull();
        });
    }

    [TestMethod]
    public async ValueTask RegistrationCancelled_CreatesCancelledEntryWithReason()
    {
        var registrationId = RegistrationId.New();
        var teamId = TeamId.New();
        var eventId = TicketedEventId.New();
        var occurredOn = DateTimeOffset.UtcNow.AddMinutes(-3);

        var handler = new WriteActivityLogHandler(Environment.RegistrationsDatabase.Context);
        await handler.HandleAsync(
            new WriteActivityLogCommand(
                teamId,
                eventId,
                registrationId,
                ActivityType.Cancelled,
                occurredOn,
                Metadata: CancellationReason.VisaLetterDenied.ToString()),
            testContext.CancellationToken);

        await Environment.RegistrationsDatabase.AssertAsync(async db =>
        {
            var entry = await db.ActivityLog
                .SingleOrDefaultAsync(
                    a => a.RegistrationId == registrationId,
                    testContext.CancellationToken);
            entry.ShouldNotBeNull();
            entry.ActivityType.ShouldBe(ActivityType.Cancelled);
            entry.OccurredAt.ShouldBe(occurredOn);
            entry.Metadata.ShouldBe("VisaLetterDenied");
        });
    }

    [TestMethod]
    public async ValueTask MultipleEntriesForSameRegistration_AllEntriesAccumulate()
    {
        var registrationId = RegistrationId.New();
        var teamId = TeamId.New();
        var eventId = TicketedEventId.New();
        var now = DateTimeOffset.UtcNow;

        var handler = new WriteActivityLogHandler(Environment.RegistrationsDatabase.Context);
        await handler.HandleAsync(
            new WriteActivityLogCommand(teamId, eventId, registrationId, ActivityType.Registered, now.AddMinutes(-10)),
            testContext.CancellationToken);
        await handler.HandleAsync(
            new WriteActivityLogCommand(teamId, eventId, registrationId, ActivityType.Reconfirmed, now.AddMinutes(-1)),
            testContext.CancellationToken);

        await Environment.RegistrationsDatabase.AssertAsync(async db =>
        {
            var entries = await db.ActivityLog
                .Where(a => a.RegistrationId == registrationId)
                .ToListAsync(testContext.CancellationToken);
            entries.Count.ShouldBe(2);
            entries.ShouldContain(a => a.ActivityType == ActivityType.Registered);
            entries.ShouldContain(a => a.ActivityType == ActivityType.Reconfirmed);
        });
    }
}
