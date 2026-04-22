using Amolenk.Admitto.Module.Registrations.Application.UseCases.TicketedEventManagement.UpdateTicketedEventDetails;
using Amolenk.Admitto.Module.Registrations.Tests.Application.Aspire;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;
using Amolenk.Admitto.Testing.Infrastructure.Assertions;
using Microsoft.EntityFrameworkCore;

namespace Amolenk.Admitto.Module.Registrations.Tests.Application.UseCases.TicketedEventManagement.UpdateTicketedEventDetails;

[TestClass]
public sealed class UpdateTicketedEventDetailsTests(TestContext testContext) : AspireIntegrationTestBase
{
    // SC-001: Update details of active event — new name and dates persist
    [TestMethod]
    public async ValueTask SC001_UpdateTicketedEventDetails_ActiveEvent_UpdatesFields()
    {
        var fixture = UpdateTicketedEventDetailsFixture.ActiveEvent();
        await fixture.SetupAsync(Environment);

        var newStart = DateTimeOffset.UtcNow.AddDays(10);
        var newEnd = newStart.AddHours(4);

        var command = new UpdateTicketedEventDetailsCommand(
            fixture.EventId,
            ExpectedVersion: fixture.SeededVersion,
            DisplayName.From("New Name"),
            AbsoluteUrl.From("https://example.com"),
            AbsoluteUrl.From("https://tickets.example.com"),
            newStart,
            newEnd);

        var sut = new UpdateTicketedEventDetailsHandler(Environment.Database.Context);

        await sut.HandleAsync(command, testContext.CancellationToken);

        await Environment.Database.AssertAsync(async ctx =>
        {
            var te = await ctx.TicketedEvents
                .FirstOrDefaultAsync(e => e.Id == fixture.EventId, testContext.CancellationToken);
            te.ShouldNotBeNull();
            te.Name.Value.ShouldBe("New Name");
            te.StartsAt.ShouldBe(newStart);
            te.EndsAt.ShouldBe(newEnd);
        });
    }

    // SC-002: Version mismatch throws concurrency conflict
    [TestMethod]
    public async ValueTask SC002_UpdateTicketedEventDetails_VersionMismatch_ThrowsConcurrencyConflict()
    {
        var fixture = UpdateTicketedEventDetailsFixture.ActiveEvent();
        await fixture.SetupAsync(Environment);

        var command = new UpdateTicketedEventDetailsCommand(
            fixture.EventId,
            ExpectedVersion: fixture.SeededVersion + 99u,
            DisplayName.From("New Name"),
            AbsoluteUrl.From("https://example.com"),
            AbsoluteUrl.From("https://tickets.example.com"),
            DateTimeOffset.UtcNow.AddDays(10),
            DateTimeOffset.UtcNow.AddDays(11));

        var sut = new UpdateTicketedEventDetailsHandler(Environment.Database.Context);

        var result = await ErrorResult.CaptureAsync(async () =>
            await sut.HandleAsync(command, testContext.CancellationToken));

        result.Error.Code.ShouldBe("concurrency_conflict");
    }

    // SC-003: Updating cancelled event throws (guard: EnsureActive)
    [TestMethod]
    public async ValueTask SC003_UpdateTicketedEventDetails_CancelledEvent_ThrowsEventNotActive()
    {
        var fixture = UpdateTicketedEventDetailsFixture.CancelledEvent();
        await fixture.SetupAsync(Environment);

        var command = new UpdateTicketedEventDetailsCommand(
            fixture.EventId,
            ExpectedVersion: fixture.SeededVersion,
            DisplayName.From("New Name"),
            AbsoluteUrl.From("https://example.com"),
            AbsoluteUrl.From("https://tickets.example.com"),
            DateTimeOffset.UtcNow.AddDays(10),
            DateTimeOffset.UtcNow.AddDays(11));

        var sut = new UpdateTicketedEventDetailsHandler(Environment.Database.Context);

        var result = await ErrorResult.CaptureAsync(async () =>
            await sut.HandleAsync(command, testContext.CancellationToken));

        result.Error.Code.ShouldBe("ticketed_event.event_not_active");
    }
}
