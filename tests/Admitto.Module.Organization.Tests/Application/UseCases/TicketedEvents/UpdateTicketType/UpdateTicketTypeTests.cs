using Amolenk.Admitto.Module.Organization.Tests.Application.Infrastructure;
using Amolenk.Admitto.Module.Organization.Application.UseCases.TicketedEvents.UpdateTicketType;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Shouldly;

namespace Amolenk.Admitto.Module.Organization.Tests.Application.UseCases.TicketedEvents.UpdateTicketType;

[TestClass]
public sealed class UpdateTicketTypeTests(TestContext testContext) : AspireIntegrationTestBase
{
    [TestMethod]
    public async ValueTask SC017_UpdateTicketType_ValidUpdate_UpdatesCapacityAndAvailability()
    {
        // Arrange
        // SC-017: Given an active event with an active ticket type, when an organizer updates
        // the ticket type's capacity and availability, the changes are persisted.
        var fixture = UpdateTicketTypeFixture.ActiveEventWithTicketType();
        await fixture.SetupAsync(Environment);

        var command = new UpdateTicketTypeCommand(
            TeamId: fixture.TeamId,
            EventId: fixture.EventId,
            ExpectedVersion: fixture.EventVersion,
            TicketTypeSlug: fixture.TicketTypeSlug,
            Name: null,
            Capacity: 150);

        var sut = new UpdateTicketTypeHandler(Environment.Database.Context);

        // Act
        await sut.HandleAsync(command, testContext.CancellationToken);

        // Assert
        await Environment.Database.AssertAsync(async dbContext =>
        {
            var ticketedEvent = await dbContext.TicketedEvents.FindAsync(
                [TicketedEventId.From(fixture.EventId)],
                testContext.CancellationToken);

            ticketedEvent.ShouldNotBeNull();

            var ticketType = ticketedEvent.TicketTypes.Single(tt => tt.Slug.Value == fixture.TicketTypeSlug);
            ticketType.Capacity?.Value.ShouldBe(150);
            ticketType.Name.Value.ShouldBe("General Admission");
        });
    }
}
