using Amolenk.Admitto.Module.Organization.Application.UseCases.TicketedEventManagement.RegisterTicketedEventCreationRejected;
using Amolenk.Admitto.Module.Organization.Tests.Application.Builders;
using Amolenk.Admitto.Module.Organization.Tests.Application.Infrastructure;
using Amolenk.Admitto.Module.Organization.Domain.Entities;
using Amolenk.Admitto.Module.Organization.Domain.ValueObjects;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Module.Organization.Tests.Application.UseCases.TicketedEventManagement.RegisterTicketedEventCreationRejected;

[TestClass]
public sealed class RegisterTicketedEventCreationRejectedTests(TestContext testContext) : AspireIntegrationTestBase
{
    [TestMethod]
    public async ValueTask IsIdempotent_OnRedelivery()
    {
        // Arrange
        var team = new TeamBuilder().Build();
        var pendingRequest = team.RequestEventCreation(
            Slug.From("e1"),
            UserId.New(),
            DateTimeOffset.UtcNow);

        await Environment.Database.SeedAsync(ctx => ctx.Teams.Add(team));

        var command = new RegisterTicketedEventCreationRejectedCommand(
            team.Id.Value,
            pendingRequest.Id.Value,
            "duplicate_slug");

        var sut = new RegisterTicketedEventCreationRejectedHandler(Environment.Database.Context);

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
            persisted.PendingEventCount.ShouldBe(0);

            var request = persisted.EventCreationRequests.ShouldHaveSingleItem();
            request.Status.ShouldBe(TeamEventCreationRequestStatus.Rejected);
            request.RejectionReason.ShouldBe("duplicate_slug");
        });
    }
}
