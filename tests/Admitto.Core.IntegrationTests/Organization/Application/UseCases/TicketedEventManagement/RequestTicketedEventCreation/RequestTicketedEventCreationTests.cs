using Amolenk.Admitto.Core.Organization.Application.UseCases.TicketedEventManagement.RequestTicketedEventCreation;
using Amolenk.Admitto.Core.Organization.Domain.ValueObjects;
using Amolenk.Admitto.Core.Shared.Kernel.ErrorHandling;
using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;
using Amolenk.Admitto.Testing.Builders.Organization.Application;
using Should = Shouldly.Should;

namespace Amolenk.Admitto.Core.IntegrationTests.Organization.Application.UseCases.TicketedEventManagement.RequestTicketedEventCreation;

[TestClass]
public sealed class RequestTicketedEventCreationTests(TestContext testContext) : AspireIntegrationTestBase
{
    [TestMethod]
    public async ValueTask AcceptsRequest_OnActiveTeam_PersistsPendingRequestAndIncrementsCounter()
    {
        // Arrange
        var team = new TeamBuilder().Build();
        await Environment.OrganizationDatabase.SeedAsync(ctx => ctx.Teams.Add(team));

        var command = new RequestTicketedEventCreationCommand(
            team.Id.Value,
            RequesterId: Guid.NewGuid(),
            Name: "Spring Conference",
            WebsiteUrl: "https://conf.example.com",
            BaseUrl: "https://tickets.example.com",
            StartsAt: DateTimeOffset.UtcNow.AddDays(7),
            EndsAt: DateTimeOffset.UtcNow.AddDays(8),
            TimeZone: "UTC");

        var sut = new RequestTicketedEventCreationHandler(Environment.OrganizationDatabase.Context);

        // Act
        var creationRequestId = await sut.HandleAsync(command, testContext.CancellationToken);

        // Assert
        creationRequestId.ShouldNotBe(Guid.Empty);

        await Environment.OrganizationDatabase.AssertAsync(async dbContext =>
        {
            var persisted = await dbContext.Teams.FindAsync(
                [TeamId.From(team.Id.Value)],
                testContext.CancellationToken);

            persisted.ShouldNotBeNull();
            persisted.PendingEventCount.ShouldBe(1);

            var request = persisted.EventCreationRequests.ShouldHaveSingleItem();
            request.Id.Value.ShouldBe(creationRequestId);
            request.Status.ShouldBe(TeamEventCreationRequestStatus.Pending);
        });
    }

    [TestMethod]
    public async ValueTask RejectsRequest_OnArchivedTeam_ThrowsTeamArchived()
    {
        // Arrange
        var team = new TeamBuilder().AsArchived().Build();
        await Environment.OrganizationDatabase.SeedAsync(ctx => ctx.Teams.Add(team));

        var command = new RequestTicketedEventCreationCommand(
            team.Id.Value,
            RequesterId: Guid.NewGuid(),
            Name: "Spring Conference",
            WebsiteUrl: "https://conf.example.com",
            BaseUrl: "https://tickets.example.com",
            StartsAt: DateTimeOffset.UtcNow.AddDays(7),
            EndsAt: DateTimeOffset.UtcNow.AddDays(8),
            TimeZone: "UTC");

        var sut = new RequestTicketedEventCreationHandler(Environment.OrganizationDatabase.Context);

        // Act & Assert
        var ex = await Should.ThrowAsync<BusinessRuleViolationException>(
            async () => await sut.HandleAsync(command, testContext.CancellationToken));

        ex.Error.Code.ShouldBe("team.archived");
    }
}
