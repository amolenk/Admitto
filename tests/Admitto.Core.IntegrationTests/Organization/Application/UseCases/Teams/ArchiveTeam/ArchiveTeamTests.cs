using Amolenk.Admitto.Core.Organization.Application.UseCases.Teams.ArchiveTeam;
using Amolenk.Admitto.Core.Organization.Domain.Entities;
using Amolenk.Admitto.Core.Shared.Kernel.ErrorHandling;
using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;
using Amolenk.Admitto.Testing.Infrastructure.Assertions;
using Should = Shouldly.Should;

namespace Amolenk.Admitto.Core.IntegrationTests.Organization.Application.UseCases.Teams.ArchiveTeam;

[TestClass]
public sealed class ArchiveTeamTests(TestContext testContext) : AspireIntegrationTestBase
{
    // Given an active team with no active ticketed events
    // When the team is archived
    // Then the team's status changes to archived
    [TestMethod]
    public async ValueTask ArchiveTeam_ActiveTeamNoEvents_ArchivesTeam()
    {
        // Arrange
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

    // Given a team that is already archived
    // When the owner attempts to archive it again
    // Then the request is rejected with an already-archived error
    [TestMethod]
    public async ValueTask ArchiveTeam_AlreadyArchivedTeam_ThrowsAlreadyArchived()
    {
        // Arrange
        var fixture = ArchiveTeamFixture.AlreadyArchivedTeam();
        await fixture.SetupAsync(Environment);

        var command = new ArchiveTeamCommand(fixture.TeamId, fixture.TeamVersion);
        var sut = new ArchiveTeamHandler(Environment.OrganizationDatabase.Context);

        // Act & Assert
        var exception = await Should.ThrowAsync<BusinessRuleViolationException>(
            async () => await sut.HandleAsync(command, testContext.CancellationToken));

        exception.Error.ShouldMatch(Team.Errors.TeamAlreadyArchived(TeamId.From(fixture.TeamId)));
    }

    // Given a team with an upcoming ticketed event
    // When the owner attempts to archive it
    // Then the request is rejected and the team remains active
    [TestMethod]
    public async ValueTask ArchiveTeam_HasActiveEvents_ThrowsHasActiveEvents()
    {
        // Arrange
        var fixture = ArchiveTeamFixture.ActiveTeamWithUpcomingEvent();
        await fixture.SetupAsync(Environment);

        var command = new ArchiveTeamCommand(fixture.TeamId, fixture.TeamVersion);
        var sut = new ArchiveTeamHandler(Environment.OrganizationDatabase.Context);

        // Act & Assert
        var exception = await Should.ThrowAsync<BusinessRuleViolationException>(
            async () => await sut.HandleAsync(command, testContext.CancellationToken));

        exception.Error.ShouldMatch(
            Team.Errors.HasActiveOrPendingEvents(TeamId.From(fixture.TeamId), active: 1, pending: 0));

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

    // Given a team with a pending ticketed event creation request not yet acknowledged
    // When the owner attempts to archive it
    // Then the request is rejected and the team remains active with the pending count preserved
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

        exception.Error.ShouldMatch(
            Team.Errors.HasActiveOrPendingEvents(TeamId.From(fixture.TeamId), active: 0, pending: 1));

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
