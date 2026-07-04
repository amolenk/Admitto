using Amolenk.Admitto.Core.Organization.Domain.ValueObjects;
using Amolenk.Admitto.Core.Shared.Infrastructure.Persistence.Inbox;
using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Core.IntegrationTests.Organization.Application.UseCases.TicketedEvents.RegisterTicketedEventCreated;

[TestClass]
public sealed class RegisterTicketedEventCreatedTests(TestContext testContext) : AspireIntegrationTestBase
{
    [TestMethod]
    public async ValueTask IsIdempotent_OnRedelivery()
    {
        // Arrange
        var fixture = RegisterTicketedEventCreatedFixture.PendingRequest();
        await fixture.SetupAsync(Environment, testContext.CancellationToken);

        var command = fixture.ToCommand();
        var sut = fixture.CreateHandler(Environment);

        // Act: deliver twice to exercise idempotency
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
            persisted.ActiveEventCount.ShouldBe(1);

            var request = persisted.EventCreationRequests.ShouldHaveSingleItem();
            request.Status.ShouldBe(TeamEventCreationRequestStatus.Created);
            request.TicketedEventId!.Value.Value.ShouldBe(fixture.TicketedEventId);
        });
    }

    [TestMethod]
    public async ValueTask HandleAsync_AlreadyProcessed_DoesNotRegisterEventCreatedAgain()
    {
        // Arrange
        var fixture = RegisterTicketedEventCreatedFixture.AlreadyProcessed();
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
            persisted.ActiveEventCount.ShouldBe(0);
        });
    }

    [TestMethod]
    public async ValueTask SaveChangesAsync_DuplicateInboxMarker_ThrowsDuplicateProcessedMessageException()
    {
        // Arrange
        var fixture = RegisterTicketedEventCreatedFixture.PendingRequest();
        await fixture.SetupAsync(Environment, testContext.CancellationToken);

        var sut = fixture.CreateIntegrationEventHandler(Environment);
        await sut.HandleAsync(fixture.IntegrationEvent, testContext.CancellationToken);
        await fixture.MarkAsConcurrentlyProcessedAsync(Environment, testContext.CancellationToken);

        var unitOfWork = fixture.CreateUnitOfWork(Environment);

        // Act
        var result = await Should.ThrowAsync<DuplicateProcessedMessageException>(async () =>
            await unitOfWork.SaveChangesAsync(testContext.CancellationToken));

        // Assert
        result.Message.ShouldBe("The message has already been processed by this handler.");
    }
}
