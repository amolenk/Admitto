using Amolenk.Admitto.Core.Registrations.Application.UseCases.TicketedEvents.UpdateTicketedEventDetails;
using Amolenk.Admitto.Testing.Infrastructure.Assertions;
using Microsoft.EntityFrameworkCore;

namespace Amolenk.Admitto.Core.IntegrationTests.Registrations.Application.UseCases.TicketedEvents.UpdateTicketedEventDetails;

[TestClass]
public sealed class UpdateTicketedEventDetailsTests(TestContext testContext) : AspireIntegrationTestBase
{
    // SC-001: Update details of active event — new name and dates persist
    [TestMethod]
    public async ValueTask UpdateTicketedEventDetails_ActiveEvent_UpdatesFields()
    {
        var fixture = UpdateTicketedEventDetailsFixture.ActiveEvent();
        await fixture.SetupAsync(Environment);

        var newStart = DateTimeOffset.UtcNow.AddDays(10);
        var newEnd = newStart.AddHours(4);

        var command = new UpdateTicketedEventDetailsCommand(
            fixture.EventId.Value,
            fixture.TeamId.Value,
            ExpectedVersion: fixture.SeededVersion,
            "New Name",
            "https://example.com",
            "https://tickets.example.com",
            "Europe/Amsterdam",
            newStart,
            newEnd);

        var sut = new UpdateTicketedEventDetailsHandler(Environment.RegistrationsDatabase.Context);

        await sut.HandleAsync(command, testContext.CancellationToken);

        await Environment.RegistrationsDatabase.AssertAsync(async ctx =>
        {
            var te = await ctx.TicketedEvents
                .FirstOrDefaultAsync(e => e.Id == fixture.EventId, testContext.CancellationToken);
            te.ShouldNotBeNull();
            te.Name.Value.ShouldBe("New Name");
            te.TimeZone.Value.ShouldBe("Europe/Amsterdam");
            te.StartsAt.ShouldBe(newStart);
            te.EndsAt.ShouldBe(newEnd);
        });
    }

    // SC-002: Version mismatch throws concurrency conflict
    [TestMethod]
    public async ValueTask UpdateTicketedEventDetails_VersionMismatch_ThrowsConcurrencyConflict()
    {
        var fixture = UpdateTicketedEventDetailsFixture.ActiveEvent();
        await fixture.SetupAsync(Environment);

        var command = new UpdateTicketedEventDetailsCommand(
            fixture.EventId.Value,
            fixture.TeamId.Value,
            ExpectedVersion: fixture.SeededVersion + 99u,
            "New Name",
            "https://example.com",
            "https://tickets.example.com",
            "Europe/Amsterdam",
            DateTimeOffset.UtcNow.AddDays(10),
            DateTimeOffset.UtcNow.AddDays(11));

        var sut = new UpdateTicketedEventDetailsHandler(Environment.RegistrationsDatabase.Context);

        var result = await ErrorResult.CaptureAsync(async () =>
            await sut.HandleAsync(command, testContext.CancellationToken));

        result.Error.Code.ShouldBe("concurrency_conflict");
    }

    // SC-003: Updating cancelled event throws (guard: EnsureActive)
    [TestMethod]
    public async ValueTask UpdateTicketedEventDetails_ArchivedEvent_ThrowsEventNotActive()
    {
        var fixture = UpdateTicketedEventDetailsFixture.ArchivedEvent();
        await fixture.SetupAsync(Environment);

        var command = new UpdateTicketedEventDetailsCommand(
            fixture.EventId.Value,
            fixture.TeamId.Value,
            ExpectedVersion: fixture.SeededVersion,
            "New Name",
            "https://example.com",
            "https://tickets.example.com",
            "Europe/Amsterdam",
            DateTimeOffset.UtcNow.AddDays(10),
            DateTimeOffset.UtcNow.AddDays(11));

        var sut = new UpdateTicketedEventDetailsHandler(Environment.RegistrationsDatabase.Context);

        var result = await ErrorResult.CaptureAsync(async () =>
            await sut.HandleAsync(command, testContext.CancellationToken));

        result.Error.Code.ShouldBe("ticketed_event.event_not_active");
    }
}
