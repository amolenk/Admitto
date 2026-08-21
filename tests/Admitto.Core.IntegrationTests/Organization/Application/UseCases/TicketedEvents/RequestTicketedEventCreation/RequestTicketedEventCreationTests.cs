using Amolenk.Admitto.Core.Organization.Application.UseCases.TicketedEvents.RequestTicketedEventCreation;
using Amolenk.Admitto.Core.Organization.Application.UseCases.Development;
using Amolenk.Admitto.Core.Organization.Domain.Entities;
using Amolenk.Admitto.Core.Organization.Domain.ValueObjects;
using Amolenk.Admitto.Core.Shared.Kernel.ErrorHandling;
using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;
using Amolenk.Admitto.Testing.Builders.Organization.Application;
using Should = Shouldly.Should;

namespace Amolenk.Admitto.Core.IntegrationTests.Organization.Application.UseCases.TicketedEvents.RequestTicketedEventCreation;

[TestClass]
public sealed class RequestTicketedEventCreationTests(TestContext testContext) : AspireIntegrationTestBase
{
    [TestMethod]
    public void LocalDemoSeedRequestState_PendingAndCreatedRequests_AreIdempotent()
    {
        var team = new TeamBuilder().Build();
        var request = team.RequestEventCreation(
            EventName.From("Demo Event"),
            AbsoluteUrl.From("https://demo.example.com"),
            AbsoluteUrl.From("https://demo.example.com"),
            Slug.From("admitto-demo"),
            DateTimeOffset.UtcNow.AddDays(1),
            DateTimeOffset.UtcNow.AddDays(2),
            TimeZoneId.From("UTC"),
            UserId.New(),
            DateTimeOffset.UtcNow);

        LocalDemoSeedRequestState.Decide(request, Slug.From("admitto-demo"))
            .ShouldBe(LocalDemoSeedRequestDecision.AlreadyInFlight);

        team.RegisterEventCreated(request.Id, TicketedEventId.New(), DateTimeOffset.UtcNow);

        LocalDemoSeedRequestState.Decide(request, Slug.From("admitto-demo"))
            .ShouldBe(LocalDemoSeedRequestDecision.AlreadyCreated);
    }

    [TestMethod]
    public void LocalDemoSeedRequestState_RejectedRequest_IsTerminal()
    {
        var team = new TeamBuilder().Build();
        var request = team.RequestEventCreation(
            EventName.From("Demo Event"),
            AbsoluteUrl.From("https://demo.example.com"),
            AbsoluteUrl.From("https://demo.example.com"),
            Slug.From("admitto-demo"),
            DateTimeOffset.UtcNow.AddDays(1),
            DateTimeOffset.UtcNow.AddDays(2),
            TimeZoneId.From("UTC"),
            UserId.New(),
            DateTimeOffset.UtcNow);
        team.RegisterEventCreationRejected(request.Id, "rejected", DateTimeOffset.UtcNow);

        LocalDemoSeedRequestState.Decide(request, Slug.From("admitto-demo"))
            .ShouldBe(LocalDemoSeedRequestDecision.Terminal);
    }

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
            TimeZone: "UTC",
            PublicSlug: "spring-conference");

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
            TimeZone: "UTC",
            PublicSlug: "spring-conference");

        var sut = new RequestTicketedEventCreationHandler(Environment.OrganizationDatabase.Context);

        // Act & Assert
        var ex = await Should.ThrowAsync<BusinessRuleViolationException>(
            async () => await sut.HandleAsync(command, testContext.CancellationToken));

        ex.Error.Code.ShouldBe("team.archived");
    }
}
