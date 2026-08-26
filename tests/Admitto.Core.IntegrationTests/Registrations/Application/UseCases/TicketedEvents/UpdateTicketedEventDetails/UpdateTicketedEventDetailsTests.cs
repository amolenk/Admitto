using Amolenk.Admitto.Core.Registrations.Application.UseCases.TicketedEvents.UpdateTicketedEventDetails;
using Amolenk.Admitto.Core.Registrations.Domain.Entities;
using Amolenk.Admitto.Core.Shared.Kernel.ErrorHandling;
using Amolenk.Admitto.Testing.Infrastructure.Assertions;
using Microsoft.EntityFrameworkCore;

namespace Amolenk.Admitto.Core.IntegrationTests.Registrations.Application.UseCases.TicketedEvents.UpdateTicketedEventDetails;

[TestClass]
public sealed class UpdateTicketedEventDetailsTests(TestContext testContext) : AspireIntegrationTestBase
{
    // Given an active ticketed event
    // When its details are updated with a new name, dates, and time zone
    // Then the new values are persisted
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

    // Given an active ticketed event
    // When its details are updated with an outdated expected version
    // Then a concurrency conflict error is returned
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

        result.Error.ShouldMatch(ConcurrencyConflictError.Create(fixture.SeededVersion + 99u, fixture.SeededVersion));
    }

    // Given an archived ticketed event
    // When its details are updated
    // Then an event-not-active error is returned
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

        result.Error.ShouldMatch(TicketedEvent.Errors.EventNotActive);
    }
}
