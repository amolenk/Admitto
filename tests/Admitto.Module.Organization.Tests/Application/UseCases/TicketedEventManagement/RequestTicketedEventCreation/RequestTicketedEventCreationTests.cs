using Amolenk.Admitto.Module.Organization.Application.UseCases.TicketedEventManagement.RequestTicketedEventCreation;
using Amolenk.Admitto.Module.Organization.Tests.Application.Builders;
using Amolenk.Admitto.Module.Organization.Tests.Application.Infrastructure;
using Amolenk.Admitto.Module.Organization.Domain.Entities;
using Amolenk.Admitto.Module.Organization.Domain.ValueObjects;
using Amolenk.Admitto.Module.Shared.Kernel.ErrorHandling;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;
using Should = Shouldly.Should;

namespace Amolenk.Admitto.Module.Organization.Tests.Application.UseCases.TicketedEventManagement.RequestTicketedEventCreation;

[TestClass]
public sealed class RequestTicketedEventCreationTests(TestContext testContext) : AspireIntegrationTestBase
{
    [TestMethod]
    public async ValueTask AcceptsRequest_OnActiveTeam_PersistsPendingRequestAndIncrementsCounter()
    {
        // Arrange
        var team = new TeamBuilder().WithSlug("acme").Build();
        await Environment.Database.SeedAsync(ctx => ctx.Teams.Add(team));

        var command = new RequestTicketedEventCreationCommand(
            team.Id.Value,
            RequesterId: Guid.NewGuid(),
            Slug: "spring-conf",
            Name: "Spring Conference",
            WebsiteUrl: "https://conf.example.com",
            BaseUrl: "https://tickets.example.com",
            StartsAt: DateTimeOffset.UtcNow.AddDays(7),
            EndsAt: DateTimeOffset.UtcNow.AddDays(8),
            TimeZone: "UTC");

        var sut = new RequestTicketedEventCreationHandler(Environment.Database.Context);

        // Act
        var creationRequestId = await sut.HandleAsync(command, testContext.CancellationToken);

        // Assert
        creationRequestId.ShouldNotBe(Guid.Empty);

        await Environment.Database.AssertAsync(async dbContext =>
        {
            var persisted = await dbContext.Teams.FindAsync(
                [TeamId.From(team.Id.Value)],
                testContext.CancellationToken);

            persisted.ShouldNotBeNull();
            persisted.PendingEventCount.ShouldBe(1);

            var request = persisted.EventCreationRequests.ShouldHaveSingleItem();
            request.Id.Value.ShouldBe(creationRequestId);
            request.Status.ShouldBe(TeamEventCreationRequestStatus.Pending);
            request.RequestedSlug.Value.ShouldBe("spring-conf");
        });
    }

    [TestMethod]
    public async ValueTask RejectsRequest_OnArchivedTeam_ThrowsTeamArchived()
    {
        // Arrange
        var team = new TeamBuilder().WithSlug("acme").AsArchived().Build();
        await Environment.Database.SeedAsync(ctx => ctx.Teams.Add(team));

        var command = new RequestTicketedEventCreationCommand(
            team.Id.Value,
            RequesterId: Guid.NewGuid(),
            Slug: "spring-conf",
            Name: "Spring Conference",
            WebsiteUrl: "https://conf.example.com",
            BaseUrl: "https://tickets.example.com",
            StartsAt: DateTimeOffset.UtcNow.AddDays(7),
            EndsAt: DateTimeOffset.UtcNow.AddDays(8),
            TimeZone: "UTC");

        var sut = new RequestTicketedEventCreationHandler(Environment.Database.Context);

        // Act & Assert
        var ex = await Should.ThrowAsync<BusinessRuleViolationException>(
            async () => await sut.HandleAsync(command, testContext.CancellationToken));

        ex.Error.Code.ShouldBe("team.archived");
    }
}
