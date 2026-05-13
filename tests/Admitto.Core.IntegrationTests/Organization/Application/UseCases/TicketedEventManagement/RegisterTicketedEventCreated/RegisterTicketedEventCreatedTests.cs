using Amolenk.Admitto.Core.Organization.Application.UseCases.TicketedEventManagement.RegisterTicketedEventCreated;
using Amolenk.Admitto.Core.Organization.Domain.ValueObjects;
using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;
using Amolenk.Admitto.Testing.Builders.Organization.Application;

namespace Amolenk.Admitto.Core.IntegrationTests.Organization.Application.UseCases.TicketedEventManagement.RegisterTicketedEventCreated;

[TestClass]
public sealed class RegisterTicketedEventCreatedTests(TestContext testContext) : AspireIntegrationTestBase
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

        var ticketedEventId = Guid.NewGuid();
        var command = new RegisterTicketedEventCreatedCommand(
            team.Id.Value,
            pendingRequest.Id.Value,
            ticketedEventId);

        var sut = new RegisterTicketedEventCreatedHandler(Environment.OrganizationDatabase.Context);

        // Act: deliver twice to exercise idempotency
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
            persisted.ActiveEventCount.ShouldBe(1);

            var request = persisted.EventCreationRequests.ShouldHaveSingleItem();
            request.Status.ShouldBe(TeamEventCreationRequestStatus.Created);
            request.TicketedEventId!.Value.Value.ShouldBe(ticketedEventId);
        });
    }
}
