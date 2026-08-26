using Amolenk.Admitto.Core.Organization.Domain.ValueObjects;
using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Core.IntegrationTests.Organization.Application.UseCases.TicketedEvents.RegisterTicketedEventArchived;

[TestClass]
public sealed class RegisterTicketedEventArchivedTests(TestContext testContext) : AspireIntegrationTestBase
{
    // Given a team with an active ticketed event
    // When the event-archived command is handled twice for the same event
    // Then the event is counted as archived once, with active count zero and archived count one
    [TestMethod]
    public async ValueTask IsIdempotent_OnRedelivery()
    {
        // Arrange
        var fixture = RegisterTicketedEventArchivedFixture.ActiveEvent();
        await fixture.SetupAsync(Environment, testContext.CancellationToken);

        var command = fixture.ToCommand();
        var sut = fixture.CreateHandler(Environment);

        // Act
        await sut.HandleAsync(command, testContext.CancellationToken);
        await Environment.OrganizationDatabase.Context.SaveChangesAsync(testContext.CancellationToken);
        await sut.HandleAsync(command, testContext.CancellationToken);

        // Assert
        await Environment.OrganizationDatabase.AssertAsync(async ctx =>
        {
            var persisted = await ctx.Teams.FindAsync(
                [TeamId.From(fixture.TeamId)],
                testContext.CancellationToken);

            persisted.ShouldNotBeNull();
            persisted.ActiveEventCount.ShouldBe(0);
            persisted.ArchivedEventCount.ShouldBe(1);
        });
    }

    // Given the event-archived message for a team's active event has already been processed by the inbox
    // When the archived integration event is handled again
    // Then the active and archived event counts remain unchanged
    [TestMethod]
    public async ValueTask HandleAsync_AlreadyProcessed_DoesNotRegisterEventArchivedAgain()
    {
        // Arrange
        var fixture = RegisterTicketedEventArchivedFixture.AlreadyProcessed();
        await fixture.SetupAsync(Environment, testContext.CancellationToken);

        var sut = fixture.CreateIntegrationEventHandler(Environment);

        // Act
        await sut.HandleAsync(fixture.IntegrationEvent, testContext.CancellationToken);

        // Assert
        await Environment.OrganizationDatabase.AssertAsync(async ctx =>
        {
            var persisted = await ctx.Teams.FindAsync(
                [TeamId.From(fixture.TeamId)],
                testContext.CancellationToken);

            persisted.ShouldNotBeNull();
            persisted.ActiveEventCount.ShouldBe(1);
            persisted.ArchivedEventCount.ShouldBe(0);
        });
    }
}
