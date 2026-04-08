using Amolenk.Admitto.Module.Organization.Tests.Application.Infrastructure;
using Amolenk.Admitto.Module.Organization.Application.UseCases.TicketedEvents.UpdateTicketedEvent;
using Amolenk.Admitto.Module.Organization.Domain.Entities;
using Amolenk.Admitto.Module.Shared.Kernel.ErrorHandling;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;
using Amolenk.Admitto.Testing.Infrastructure.Assertions;
using Should = Shouldly.Should;
using Shouldly;

namespace Amolenk.Admitto.Module.Organization.Tests.Application.UseCases.TicketedEvents.UpdateTicketedEvent;

[TestClass]
public sealed class UpdateTicketedEventTests(TestContext testContext) : AspireIntegrationTestBase
{
    [TestMethod]
    public async ValueTask SC006_UpdateTicketedEvent_ValidUpdate_UpdatesEvent()
    {
        // Arrange
        // SC-006: Given an active ticketed event, when an organizer submits a valid update,
        // the event's name, website URL and dates are updated.
        var fixture = UpdateTicketedEventFixture.ActiveEvent();
        await fixture.SetupAsync(Environment);

        var command = new UpdateTicketedEventCommand(
            TeamId: fixture.TeamId,
            EventId: fixture.EventId,
            ExpectedVersion: fixture.EventVersion,
            Name: "Acme Conf 2026 — Updated",
            WebsiteUrl: "https://conf2.acme.org",
            BaseUrl: null,
            StartsAt: new DateTimeOffset(2026, 6, 2, 9, 0, 0, TimeSpan.Zero),
            EndsAt: new DateTimeOffset(2026, 6, 4, 17, 0, 0, TimeSpan.Zero));

        var sut = new UpdateTicketedEventHandler(Environment.Database.Context);

        // Act
        await sut.HandleAsync(command, testContext.CancellationToken);

        // Assert
        await Environment.Database.AssertAsync(async dbContext =>
        {
            var ticketedEvent = await dbContext.TicketedEvents.FindAsync(
                [TicketedEventId.From(fixture.EventId)],
                testContext.CancellationToken);

            ticketedEvent.ShouldNotBeNull();
            ticketedEvent.Name.Value.ShouldBe("Acme Conf 2026 — Updated");
            ticketedEvent.WebsiteUrl.Value.ToString().ShouldBe("https://conf2.acme.org/");
            ticketedEvent.EventWindow.Start.ShouldBe(command.StartsAt!.Value);
            ticketedEvent.EventWindow.End.ShouldBe(command.EndsAt!.Value);
        });
    }

    [TestMethod]
    public async ValueTask SC007_UpdateTicketedEvent_WrongVersion_ThrowsConcurrencyConflict()
    {
        // Arrange
        // SC-007: Given an active ticketed event, when an organizer submits an update
        // with a stale version, the request is rejected with a concurrency conflict.
        var fixture = UpdateTicketedEventFixture.ActiveEvent();
        await fixture.SetupAsync(Environment);

        var staleVersion = fixture.EventVersion - 1;
        var command = new UpdateTicketedEventCommand(
            TeamId: fixture.TeamId,
            EventId: fixture.EventId,
            ExpectedVersion: staleVersion,
            Name: "Should Not Update",
            WebsiteUrl: null,
            BaseUrl: null,
            StartsAt: null,
            EndsAt: null);

        var sut = new UpdateTicketedEventHandler(Environment.Database.Context);

        // Act & Assert
        var exception = await Should.ThrowAsync<BusinessRuleViolationException>(
            async () => await sut.HandleAsync(command, testContext.CancellationToken));

        exception.Error.Code.ShouldBe("concurrency_conflict");
    }

    [TestMethod]
    public async ValueTask SC008_UpdateTicketedEvent_CancelledEvent_ThrowsEventCancelled()
    {
        // Arrange
        // SC-008: Given a cancelled ticketed event, when an organizer attempts to update it,
        // the request is rejected because cancelled events cannot be modified.
        var fixture = UpdateTicketedEventFixture.CancelledEvent();
        await fixture.SetupAsync(Environment);

        var command = new UpdateTicketedEventCommand(
            TeamId: fixture.TeamId,
            EventId: fixture.EventId,
            ExpectedVersion: fixture.EventVersion,
            Name: "Should Not Update",
            WebsiteUrl: null,
            BaseUrl: null,
            StartsAt: null,
            EndsAt: null);

        var sut = new UpdateTicketedEventHandler(Environment.Database.Context);

        // Act & Assert
        var exception = await Should.ThrowAsync<BusinessRuleViolationException>(
            async () => await sut.HandleAsync(command, testContext.CancellationToken));

        exception.Error.ShouldMatch(TicketedEvent.Errors.EventCancelled(TicketedEventId.From(fixture.EventId)));
    }
}
