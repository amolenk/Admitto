using Amolenk.Admitto.Module.Organization.Application.UseCases.TicketedEventManagement.RegisterTicketedEventArchived;
using Amolenk.Admitto.Module.Organization.Tests.Application.Builders;
using Amolenk.Admitto.Module.Organization.Tests.Application.Infrastructure;
using Amolenk.Admitto.Module.Organization.Domain.ValueObjects;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Module.Organization.Tests.Application.UseCases.TicketedEventManagement.RegisterTicketedEventArchived;

[TestClass]
public sealed class RegisterTicketedEventArchivedTests(TestContext testContext) : AspireIntegrationTestBase
{
    [TestMethod]
    public async ValueTask IsIdempotent_OnRedelivery()
    {
        // Arrange: team with a Created request in Active state.
        var team = new TeamBuilder().Build();
        var pendingRequest = team.RequestEventCreation(Slug.From("e1"), UserId.New(), DateTimeOffset.UtcNow);
        var ticketedEventId = TicketedEventId.New();
        team.RegisterEventCreated(pendingRequest.Id, ticketedEventId, DateTimeOffset.UtcNow);

        await Environment.Database.SeedAsync(ctx => ctx.Teams.Add(team));

        var command = new RegisterTicketedEventArchivedCommand(team.Id.Value, ticketedEventId.Value);
        var sut = new RegisterTicketedEventArchivedHandler(Environment.Database.Context);

        // Act
        await sut.HandleAsync(command, testContext.CancellationToken);
        await Environment.Database.Context.SaveChangesAsync(testContext.CancellationToken);
        await sut.HandleAsync(command, testContext.CancellationToken);

        // Assert
        await Environment.Database.AssertAsync(async ctx =>
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
