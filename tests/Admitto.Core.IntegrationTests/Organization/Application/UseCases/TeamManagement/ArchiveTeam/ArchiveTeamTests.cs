using Amolenk.Admitto.Core.Organization.Application.UseCases.TeamManagement.ArchiveTeam;
using Amolenk.Admitto.Core.Organization.Domain.Entities;
using Amolenk.Admitto.Core.Shared.Kernel.ErrorHandling;
using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;
using Amolenk.Admitto.Testing.Infrastructure.Assertions;
using Should = Shouldly.Should;

namespace Amolenk.Admitto.Core.IntegrationTests.Organization.Application.UseCases.TeamManagement.ArchiveTeam;

[TestClass]
public sealed class ArchiveTeamTests(TestContext testContext) : AspireIntegrationTestBase
{
    [TestMethod]
    public async ValueTask ArchiveTeam_ActiveTeamNoEvents_ArchivesTeam()
    {
        // Arrange
        // SC-009: Given an active team "acme" with no active ticketed events,
        // when the owner archives the team, its status changes to archived.
        var fixture = ArchiveTeamFixture.ActiveTeamWithNoEvents();
        await fixture.SetupAsync(Environment);

        var command = new ArchiveTeamCommand(fixture.TeamId, fixture.TeamVersion);
        var sut = new ArchiveTeamHandler(Environment.OrganizationDatabase.Context);

        // Act
        await sut.HandleAsync(command, testContext.CancellationToken);

        // Assert
        await Environment.OrganizationDatabase.AssertAsync(async dbContext =>
        {
            var team = await dbContext.Teams.FindAsync(
                [TeamId.From(fixture.TeamId)],
                testContext.CancellationToken);

            team.ShouldNotBeNull();
            team.IsArchived.ShouldBeTrue();
            team.ArchivedAt.ShouldNotBeNull();
        });
    }

    [TestMethod]
    public async ValueTask ArchiveTeam_AlreadyArchivedTeam_ThrowsAlreadyArchived()
    {
        // Arrange
        // SC-011: Given team "acme" is already archived, when the owner attempts to
        // archive it again, the request is rejected with an "already archived" error.
        var fixture = ArchiveTeamFixture.AlreadyArchivedTeam();
        await fixture.SetupAsync(Environment);

        var command = new ArchiveTeamCommand(fixture.TeamId, fixture.TeamVersion);
        var sut = new ArchiveTeamHandler(Environment.OrganizationDatabase.Context);

        // Act & Assert
        var exception = await Should.ThrowAsync<BusinessRuleViolationException>(
            async () => await sut.HandleAsync(command, testContext.CancellationToken));

        exception.Error.ShouldMatch(Team.Errors.TeamAlreadyArchived(TeamId.From(fixture.TeamId)));
    }

    [TestMethod]
    public async ValueTask ArchiveTeam_HasActiveEvents_ThrowsHasActiveEvents()
    {
        // Arrange
        // SC-014: Given team "acme" has an upcoming ticketed event,
        // when the owner attempts to archive it, the request is rejected.
        var fixture = ArchiveTeamFixture.ActiveTeamWithUpcomingEvent();
        await fixture.SetupAsync(Environment);

        var command = new ArchiveTeamCommand(fixture.TeamId, fixture.TeamVersion);
        var sut = new ArchiveTeamHandler(Environment.OrganizationDatabase.Context);

        // Act & Assert
        var exception = await Should.ThrowAsync<BusinessRuleViolationException>(
            async () => await sut.HandleAsync(command, testContext.CancellationToken));

        exception.Error.Code.ShouldBe("team.has_active_or_pending_events");

        // Verify the team remains active
        await Environment.OrganizationDatabase.WithContextAsync(async dbContext =>
        {
            var team = await dbContext.Teams.FindAsync(
                [TeamId.From(fixture.TeamId)],
                testContext.CancellationToken);

            team.ShouldNotBeNull();
            team.IsArchived.ShouldBeFalse();
        });
    }

    [TestMethod]
    public async ValueTask ArchiveTeam_HasPendingCreationRequest_ThrowsHasActiveOrPendingEvents()
    {
        // Arrange: team has a pending TicketedEventCreationRequestedIntegrationEvent that Registrations
        // has not yet acked. Archive must be blocked by PendingEventCount > 0.
        var fixture = ArchiveTeamFixture.ActiveTeamWithPendingCreationRequest();
        await fixture.SetupAsync(Environment);

        var command = new ArchiveTeamCommand(fixture.TeamId, fixture.TeamVersion);
        var sut = new ArchiveTeamHandler(Environment.OrganizationDatabase.Context);

        // Act & Assert
        var exception = await Should.ThrowAsync<BusinessRuleViolationException>(
            async () => await sut.HandleAsync(command, testContext.CancellationToken));

        exception.Error.Code.ShouldBe("team.has_active_or_pending_events");

        await Environment.OrganizationDatabase.WithContextAsync(async dbContext =>
        {
            var team = await dbContext.Teams.FindAsync(
                [TeamId.From(fixture.TeamId)],
                testContext.CancellationToken);

            team.ShouldNotBeNull();
            team.IsArchived.ShouldBeFalse();
            team.PendingEventCount.ShouldBe(1);
        });
    }
}
