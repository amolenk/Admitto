using Amolenk.Admitto.Core.Organization.Application.UseCases.TicketedEvents.RegisterTicketedEventArchived;
using Amolenk.Admitto.Core.Organization.Domain.ValueObjects;
using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;
using Amolenk.Admitto.Testing.Builders.Organization.Application;

namespace Amolenk.Admitto.Core.IntegrationTests.Organization.Application.UseCases.TicketedEvents.RegisterTicketedEventArchived;

[TestClass]
public sealed class RegisterTicketedEventArchivedTests(TestContext testContext) : AspireIntegrationTestBase
{
    [TestMethod]
    public async ValueTask IsIdempotent_OnRedelivery()
    {
        // Arrange: team with a Created request in Active state.
        var team = new TeamBuilder().Build();
        var pendingRequest = team.RequestEventCreation(
                UserId.New(), DateTimeOffset.UtcNow);
        var ticketedEventId = TicketedEventId.New();
        team.RegisterEventCreated(pendingRequest.Id, ticketedEventId, DateTimeOffset.UtcNow);

        await Environment.OrganizationDatabase.SeedAsync(ctx => ctx.Teams.Add(team));

        var command = new RegisterTicketedEventArchivedCommand(team.Id.Value, ticketedEventId.Value);
        var sut = new RegisterTicketedEventArchivedHandler(Environment.OrganizationDatabase.Context);

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
            persisted.ActiveEventCount.ShouldBe(0);
            persisted.ArchivedEventCount.ShouldBe(1);
        });
    }
}
