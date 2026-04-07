using Amolenk.Admitto.Module.Organization.Tests.Application.Infrastructure;
using Amolenk.Admitto.Module.Organization.Application.UseCases.TicketedEvents.CancelTicketedEvent;
using Amolenk.Admitto.Module.Organization.Domain.Entities;
using Amolenk.Admitto.Module.Organization.Domain.ValueObjects;
using Amolenk.Admitto.Module.Shared.Kernel.ErrorHandling;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;
using Amolenk.Admitto.Testing.Infrastructure.Assertions;
using Should = Shouldly.Should;
using Microsoft.EntityFrameworkCore;
using Shouldly;

namespace Amolenk.Admitto.Module.Organization.Tests.Application.UseCases.TicketedEvents.CancelTicketedEvent;

[TestClass]
public sealed class CancelTicketedEventTests(TestContext testContext) : AspireIntegrationTestBase
{
    [TestMethod]
    public async ValueTask SC009_CancelTicketedEvent_ActiveEvent_CancelsEvent()
    {
        // Arrange
        // SC-009: Given an active ticketed event, when an organizer
        // cancels the event, its status becomes cancelled.
        var fixture = CancelTicketedEventFixture.ActiveEvent();
        await fixture.SetupAsync(Environment);

        var command = new CancelTicketedEventCommand(fixture.TeamId, fixture.EventId, fixture.EventVersion);
        var sut = new CancelTicketedEventHandler(Environment.Database.Context);

        // Act
        await sut.HandleAsync(command, testContext.CancellationToken);

        // Assert
        await Environment.Database.AssertAsync(async dbContext =>
        {
            var ticketedEvent = await dbContext.TicketedEvents.FindAsync(
                [TicketedEventId.From(fixture.EventId)],
                testContext.CancellationToken);

            ticketedEvent.ShouldNotBeNull();
            ticketedEvent.Status.ShouldBe(EventStatus.Cancelled);
        });
    }

    [TestMethod]
    public async ValueTask SC010_CancelTicketedEvent_AlreadyCancelledEvent_ThrowsAlreadyCancelled()
    {
        // Arrange
        // SC-010: Given an already cancelled ticketed event, when an organizer attempts to
        // cancel it again, the request is rejected.
        var fixture = CancelTicketedEventFixture.AlreadyCancelledEvent();
        await fixture.SetupAsync(Environment);

        var command = new CancelTicketedEventCommand(fixture.TeamId, fixture.EventId, fixture.EventVersion);
        var sut = new CancelTicketedEventHandler(Environment.Database.Context);

        // Act & Assert
        var exception = await Should.ThrowAsync<BusinessRuleViolationException>(
            async () => await sut.HandleAsync(command, testContext.CancellationToken));

        exception.Error.ShouldMatch(TicketedEvent.Errors.EventAlreadyCancelled(TicketedEventId.From(fixture.EventId)));
    }
}
