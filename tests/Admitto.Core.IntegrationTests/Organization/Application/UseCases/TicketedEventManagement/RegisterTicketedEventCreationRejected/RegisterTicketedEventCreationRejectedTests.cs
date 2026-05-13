using Amolenk.Admitto.Core.Organization.Application.UseCases.TicketedEventManagement.RegisterTicketedEventCreationRejected;
using Amolenk.Admitto.Core.Organization.Domain.ValueObjects;
using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;
using Amolenk.Admitto.Testing.Builders.Organization.Application;

namespace Amolenk.Admitto.Core.IntegrationTests.Organization.Application.UseCases.TicketedEventManagement.RegisterTicketedEventCreationRejected;

[TestClass]
public sealed class RegisterTicketedEventCreationRejectedTests(TestContext testContext) : AspireIntegrationTestBase
{
    [TestMethod]
    public async ValueTask IsIdempotent_OnRedelivery()
    {
        // Arrange
        var team = new TeamBuilder().Build();
        var pendingRequest = team.RequestEventCreation(
                UserId.New(),
            DateTimeOffset.UtcNow);

        await Environment.OrganizationDatabase.SeedAsync(ctx => ctx.Teams.Add(team));

        var command = new RegisterTicketedEventCreationRejectedCommand(
            team.Id.Value,
            pendingRequest.Id.Value,
            "duplicate_slug");

        var sut = new RegisterTicketedEventCreationRejectedHandler(Environment.OrganizationDatabase.Context);

        // Act
        await sut.HandleAsync(command, testContext.CancellationToken);
        await Environment.OrganizationDatabase.Context.SaveChangesAsync(testContext.CancellationToken);
        await sut.HandleAsync(command, testContext.CancellationToken);

        // Assert
        await Environment.OrganizationDatabase.AssertAsync(async ctx =>
        {
            var persisted = await ctx.Teams.FindAsync(
                [TeamId.From(team.Id.Value)],
                testContext.CancellationToken);

            persisted.ShouldNotBeNull();
            persisted.PendingEventCount.ShouldBe(0);

            var request = persisted.EventCreationRequests.ShouldHaveSingleItem();
            request.Status.ShouldBe(TeamEventCreationRequestStatus.Rejected);
            request.RejectionReason.ShouldBe("duplicate_slug");
        });
    }
}
