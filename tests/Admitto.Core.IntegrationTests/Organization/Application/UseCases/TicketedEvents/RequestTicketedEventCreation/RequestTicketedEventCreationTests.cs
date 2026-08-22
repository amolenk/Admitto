using Amolenk.Admitto.Core.Organization.Application.UseCases.TicketedEvents.RequestTicketedEventCreation;
using Amolenk.Admitto.Core.Organization.Application.UseCases.Development;
using Amolenk.Admitto.Core.Organization.Domain.Entities;
using Amolenk.Admitto.Core.Organization.Domain.ValueObjects;
using Amolenk.Admitto.Core.Shared.Kernel.ErrorHandling;
using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;
using Amolenk.Admitto.Testing.Builders.Organization.Application;
using Amolenk.Admitto.Testing.Infrastructure.Assertions;
using Should = Shouldly.Should;

namespace Amolenk.Admitto.Core.IntegrationTests.Organization.Application.UseCases.TicketedEvents.RequestTicketedEventCreation;

[TestClass]
public sealed class RequestTicketedEventCreationTests(TestContext testContext) : AspireIntegrationTestBase
{
    // Given a team's event creation request for a demo slug
    // When the demo seed decision is evaluated while the request is pending, and again after the event is created
    // Then it reports the request as already in flight while pending, and as already created afterwards
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

    // Given a team's event creation request that has been rejected
    // When the demo seed decision is evaluated for the same slug
    // Then it reports the request as terminal
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

    // Given an active team
    // When a RequestTicketedEventCreation command is handled
    // Then a pending event creation request is persisted and the team's pending event count is incremented
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

    // Given an archived team
    // When a RequestTicketedEventCreation command is handled
    // Then a team-archived error is thrown
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

        ex.Error.ShouldMatch(Team.Errors.TeamArchived(team.Id));
    }
}
