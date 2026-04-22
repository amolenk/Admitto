using Amolenk.Admitto.Module.Organization.Application.Jobs;
using Amolenk.Admitto.Module.Organization.Tests.Application.Builders;
using Amolenk.Admitto.Module.Organization.Tests.Application.Infrastructure;
using Amolenk.Admitto.Module.Organization.Tests.Application.Jobs;
using Amolenk.Admitto.Module.Organization.Domain.Entities;
using Amolenk.Admitto.Module.Organization.Domain.ValueObjects;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Quartz;

namespace Amolenk.Admitto.Module.Organization.Tests.Application.UseCases.TicketedEventManagement.Jobs;

[TestClass]
public sealed class ExpireStaleEventCreationRequestsJobTests(TestContext testContext) : AspireIntegrationTestBase
{
    [TestMethod]
    public async ValueTask ExpiresStalePendingRequest_AndDecrementsPendingCount()
    {
        // Arrange: a team with a pending request older than the timeout.
        var team = new TeamBuilder().Build();
        var staleRequest = team.RequestEventCreation(
            Slug.From("stale"),
            UserId.New(),
            DateTimeOffset.UtcNow);

        await Environment.Database.SeedAsync(ctx => ctx.Teams.Add(team));

        // Push requested_at into the past (> 24h) so the job sees it as stale.
        await Environment.Database.Context.Database.ExecuteSqlAsync(
            $"UPDATE organization.team_event_creation_requests SET requested_at = NOW() - INTERVAL '25 hours' WHERE id = {staleRequest.Id.Value}");
        Environment.Database.Context.ChangeTracker.Clear();

        var job = new ExpireStaleEventCreationRequestsJob(
            Environment.Database.Context,
            new DbContextUnitOfWork(Environment.Database.Context),
            NullLogger<ExpireStaleEventCreationRequestsJob>.Instance);

        var quartzContext = Substitute.For<IJobExecutionContext>();
        quartzContext.CancellationToken.Returns(testContext.CancellationToken);

        // Act
        await job.Execute(quartzContext);

        // Assert
        await Environment.Database.AssertAsync(async ctx =>
        {
            var persisted = await ctx.Teams.FindAsync(
                [TeamId.From(team.Id.Value)],
                testContext.CancellationToken);

            persisted.ShouldNotBeNull();
            persisted.PendingEventCount.ShouldBe(0);

            var request = persisted.EventCreationRequests.ShouldHaveSingleItem();
            request.Status.ShouldBe(TeamEventCreationRequestStatus.Expired);
            request.CompletedAt.ShouldNotBeNull();
        });
    }

    [TestMethod]
    public async ValueTask LeavesFreshPendingRequestUntouched()
    {
        // Arrange: a team with a recent pending request.
        var team = new TeamBuilder().Build();
        var freshRequest = team.RequestEventCreation(
            Slug.From("fresh"),
            UserId.New(),
            DateTimeOffset.UtcNow);

        await Environment.Database.SeedAsync(ctx => ctx.Teams.Add(team));

        var job = new ExpireStaleEventCreationRequestsJob(
            Environment.Database.Context,
            new DbContextUnitOfWork(Environment.Database.Context),
            NullLogger<ExpireStaleEventCreationRequestsJob>.Instance);

        var quartzContext = Substitute.For<IJobExecutionContext>();
        quartzContext.CancellationToken.Returns(testContext.CancellationToken);

        // Act
        await job.Execute(quartzContext);

        // Assert
        await Environment.Database.AssertAsync(async ctx =>
        {
            var persisted = await ctx.Teams.FindAsync(
                [TeamId.From(team.Id.Value)],
                testContext.CancellationToken);

            persisted.ShouldNotBeNull();
            persisted.PendingEventCount.ShouldBe(1);

            var request = persisted.EventCreationRequests.ShouldHaveSingleItem();
            request.Status.ShouldBe(TeamEventCreationRequestStatus.Pending);
        });
    }
}
