using Amolenk.Admitto.Core.Organization.Domain.ValueObjects;
using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Core.IntegrationTests.Organization.Application.UseCases.TicketedEvents.RegisterTicketedEventCreationRejected;

[TestClass]
public sealed class RegisterTicketedEventCreationRejectedTests(TestContext testContext) : AspireIntegrationTestBase
{
    // Given a team with a pending event creation request
    // When the rejection command is handled twice for the same request
    // Then the request is rejected once with the given reason and the pending count drops to zero
    [TestMethod]
    public async ValueTask IsIdempotent_OnRedelivery()
    {
        // Arrange
        var fixture = RegisterTicketedEventCreationRejectedFixture.PendingRequest();
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
            persisted.PendingEventCount.ShouldBe(0);

            var request = persisted.EventCreationRequests.ShouldHaveSingleItem();
            request.Status.ShouldBe(TeamEventCreationRequestStatus.Rejected);
            request.RejectionReason.ShouldBe(fixture.Reason);
        });
    }

    // Given the rejection message for a pending event creation request has already been processed by the inbox
    // When the rejection integration event is handled again
    // Then the request remains pending and the pending event count is unchanged
    [TestMethod]
    public async ValueTask HandleAsync_AlreadyProcessed_DoesNotRegisterEventCreationRejectedAgain()
    {
        // Arrange
        var fixture = RegisterTicketedEventCreationRejectedFixture.AlreadyProcessed();
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
            persisted.PendingEventCount.ShouldBe(1);

            var request = persisted.EventCreationRequests.ShouldHaveSingleItem();
            request.Status.ShouldBe(TeamEventCreationRequestStatus.Pending);
        });
    }
}
