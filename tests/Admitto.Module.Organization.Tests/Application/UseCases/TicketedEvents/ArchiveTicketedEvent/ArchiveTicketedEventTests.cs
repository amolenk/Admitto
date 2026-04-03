using Amolenk.Admitto.Module.Organization.Tests.Application.Infrastructure;
using Amolenk.Admitto.Module.Organization.Application.UseCases.TicketedEvents.ArchiveTicketedEvent;
using Amolenk.Admitto.Module.Organization.Domain.Entities;
using Amolenk.Admitto.Module.Organization.Domain.ValueObjects;
using Amolenk.Admitto.Module.Shared.Kernel.ErrorHandling;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;
using Amolenk.Admitto.Testing.Infrastructure.Assertions;
using Amolenk.Admitto.Testing.Infrastructure.Assertions;
using Should = Shouldly.Should;
using Shouldly;

namespace Amolenk.Admitto.Module.Organization.Tests.Application.UseCases.TicketedEvents.ArchiveTicketedEvent;

[TestClass]
public sealed class ArchiveTicketedEventTests(TestContext testContext) : AspireIntegrationTestBase
{
    [TestMethod]
    public async ValueTask SC011_ArchiveTicketedEvent_ActiveEvent_ChangesStatusToArchived()
    {
        // Arrange
        // SC-011: Given an active ticketed event, when an organizer archives it,
        // the event's status changes to Archived.
        var fixture = ArchiveTicketedEventFixture.ActiveEvent();
        await fixture.SetupAsync(Environment);

        var command = new ArchiveTicketedEventCommand(fixture.TeamId, fixture.EventId, fixture.EventVersion);
        var sut = new ArchiveTicketedEventHandler(Environment.Database.Context);

        // Act
        await sut.HandleAsync(command, testContext.CancellationToken);

        // Assert
        await Environment.Database.AssertAsync(async dbContext =>
        {
            var ticketedEvent = await dbContext.TicketedEvents.FindAsync(
                [TicketedEventId.From(fixture.EventId)],
                testContext.CancellationToken);

            ticketedEvent.ShouldNotBeNull();
            ticketedEvent.Status.ShouldBe(EventStatus.Archived);
        });
    }

    [TestMethod]
    public async ValueTask SC012_ArchiveTicketedEvent_CancelledEvent_ChangesStatusToArchived()
    {
        // Arrange
        // SC-012: Given a cancelled ticketed event, when an organizer archives it,
        // the event's status changes to Archived.
        var fixture = ArchiveTicketedEventFixture.CancelledEvent();
        await fixture.SetupAsync(Environment);

        var command = new ArchiveTicketedEventCommand(fixture.TeamId, fixture.EventId, fixture.EventVersion);
        var sut = new ArchiveTicketedEventHandler(Environment.Database.Context);

        // Act
        await sut.HandleAsync(command, testContext.CancellationToken);

        // Assert
        await Environment.Database.AssertAsync(async dbContext =>
        {
            var ticketedEvent = await dbContext.TicketedEvents.FindAsync(
                [TicketedEventId.From(fixture.EventId)],
                testContext.CancellationToken);

            ticketedEvent.ShouldNotBeNull();
            ticketedEvent.Status.ShouldBe(EventStatus.Archived);
        });
    }

    [TestMethod]
    public async ValueTask SC013_ArchiveTicketedEvent_ArchivedEvent_ThrowsAlreadyArchived()
    {
        // Arrange
        // SC-013: Given an already archived ticketed event, when an organizer attempts to
        // archive it again, the request is rejected.
        var fixture = ArchiveTicketedEventFixture.AlreadyArchivedEvent();
        await fixture.SetupAsync(Environment);

        var command = new ArchiveTicketedEventCommand(fixture.TeamId, fixture.EventId, fixture.EventVersion);
        var sut = new ArchiveTicketedEventHandler(Environment.Database.Context);

        // Act & Assert
        var exception = await Should.ThrowAsync<BusinessRuleViolationException>(
            async () => await sut.HandleAsync(command, testContext.CancellationToken));

        exception.Error.ShouldMatch(TicketedEvent.Errors.EventAlreadyArchived(TicketedEventId.From(fixture.EventId)));
    }
}
