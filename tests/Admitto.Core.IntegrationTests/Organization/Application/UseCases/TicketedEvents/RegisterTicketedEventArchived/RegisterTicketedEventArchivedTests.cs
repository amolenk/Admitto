using Amolenk.Admitto.Core.Organization.Domain.ValueObjects;
using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Core.IntegrationTests.Organization.Application.UseCases.TicketedEvents.RegisterTicketedEventArchived;

[TestClass]
public sealed class RegisterTicketedEventArchivedTests(TestContext testContext) : AspireIntegrationTestBase
{
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
