using Amolenk.Admitto.Module.Registrations.Application.UseCases.Registrations.WriteActivityLog;
using Amolenk.Admitto.Module.Registrations.Domain.ValueObjects;
using Amolenk.Admitto.Module.Registrations.Tests.Application.Aspire;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace Amolenk.Admitto.Module.Registrations.Tests.Application.UseCases.Registrations.WriteActivityLog;

[TestClass]
public sealed class WriteActivityLogHandlerTests(TestContext testContext) : AspireIntegrationTestBase
{
    [TestMethod]
    public async ValueTask SC001_AttendeeRegistered_CreatesRegisteredEntry()
    {
        var registrationId = RegistrationId.New();
        var occurredOn = DateTimeOffset.UtcNow.AddMinutes(-10);

        var handler = new WriteActivityLogHandler(Environment.Database.Context);
        await handler.HandleAsync(
            new WriteActivityLogCommand(registrationId, ActivityType.Registered, occurredOn),
            testContext.CancellationToken);

        await Environment.Database.AssertAsync(async db =>
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
    public async ValueTask SC002_RegistrationReconfirmed_CreatesReconfirmedEntry()
    {
        var registrationId = RegistrationId.New();
        var reconfirmedAt = DateTimeOffset.UtcNow.AddMinutes(-5);

        var handler = new WriteActivityLogHandler(Environment.Database.Context);
        await handler.HandleAsync(
            new WriteActivityLogCommand(registrationId, ActivityType.Reconfirmed, reconfirmedAt),
            testContext.CancellationToken);

        await Environment.Database.AssertAsync(async db =>
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
    public async ValueTask SC003_RegistrationCancelled_CreatesCancelledEntryWithReason()
    {
        var registrationId = RegistrationId.New();
        var occurredOn = DateTimeOffset.UtcNow.AddMinutes(-3);

        var handler = new WriteActivityLogHandler(Environment.Database.Context);
        await handler.HandleAsync(
            new WriteActivityLogCommand(
                registrationId,
                ActivityType.Cancelled,
                occurredOn,
                Metadata: CancellationReason.VisaLetterDenied.ToString()),
            testContext.CancellationToken);

        await Environment.Database.AssertAsync(async db =>
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
    public async ValueTask SC004_MultipleEntriesForSameRegistration_AllEntriesAccumulate()
    {
        var registrationId = RegistrationId.New();
        var now = DateTimeOffset.UtcNow;

        var handler = new WriteActivityLogHandler(Environment.Database.Context);
        await handler.HandleAsync(
            new WriteActivityLogCommand(registrationId, ActivityType.Registered, now.AddMinutes(-10)),
            testContext.CancellationToken);
        await handler.HandleAsync(
            new WriteActivityLogCommand(registrationId, ActivityType.Reconfirmed, now.AddMinutes(-1)),
            testContext.CancellationToken);

        await Environment.Database.AssertAsync(async db =>
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
