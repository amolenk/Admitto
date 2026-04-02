using Amolenk.Admitto.Module.Organization.Tests.Application.Infrastructure;
using Amolenk.Admitto.Module.Organization.Application.UseCases.TeamManagement.ArchiveTeam;
using Amolenk.Admitto.Module.Shared.Kernel.ErrorHandling;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;
using Amolenk.Admitto.Testing.Infrastructure.Assertions;
using Should = Shouldly.Should;

namespace Amolenk.Admitto.Module.Organization.Tests.Application.UseCases.TeamManagement.ArchiveTeam;

[TestClass]
public sealed class ArchiveTeamTests(TestContext testContext) : AspireIntegrationTestBase
{
    [TestMethod]
    public async ValueTask SC009_ArchiveTeam_ActiveTeamNoEvents_ArchivesTeam()
    {
        // Arrange
        // SC-009: Given an active team "acme" with no active ticketed events,
        // when the owner archives the team, its status changes to archived.
        var fixture = ArchiveTeamFixture.ActiveTeamWithNoEvents();
        await fixture.SetupAsync(Environment);

        var command = new ArchiveTeamCommand(fixture.TeamId, fixture.TeamVersion);
        var sut = new ArchiveTeamHandler(Environment.Database.Context);

        // Act
        await sut.HandleAsync(command, testContext.CancellationToken);

        // Assert
        await Environment.Database.AssertAsync(async dbContext =>
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
    public async ValueTask SC011_ArchiveTeam_AlreadyArchivedTeam_ThrowsAlreadyArchived()
    {
        // Arrange
        // SC-011: Given team "acme" is already archived, when the owner attempts to
        // archive it again, the request is rejected with an "already archived" error.
        var fixture = ArchiveTeamFixture.AlreadyArchivedTeam();
        await fixture.SetupAsync(Environment);

        var command = new ArchiveTeamCommand(fixture.TeamId, fixture.TeamVersion);
        var sut = new ArchiveTeamHandler(Environment.Database.Context);

        // Act & Assert
        var exception = await Should.ThrowAsync<BusinessRuleViolationException>(
            async () => await sut.HandleAsync(command, testContext.CancellationToken));

        exception.Error.Code.ShouldBe("team.already_archived");
    }

    [TestMethod]
    public async ValueTask SC014_ArchiveTeam_HasActiveEvents_ThrowsHasActiveEvents()
    {
        // Arrange
        // SC-014: Given team "acme" has an upcoming ticketed event,
        // when the owner attempts to archive it, the request is rejected.
        var fixture = ArchiveTeamFixture.ActiveTeamWithUpcomingEvent();
        await fixture.SetupAsync(Environment);

        var command = new ArchiveTeamCommand(fixture.TeamId, fixture.TeamVersion);
        var sut = new ArchiveTeamHandler(Environment.Database.Context);

        // Act & Assert
        var exception = await Should.ThrowAsync<BusinessRuleViolationException>(
            async () => await sut.HandleAsync(command, testContext.CancellationToken));

        exception.Error.ShouldMatch(ArchiveTeamHandler.Errors.HasActiveEvents);

        // Verify the team remains active
        await Environment.Database.WithContextAsync(async dbContext =>
        {
            var team = await dbContext.Teams.FindAsync(
                [TeamId.From(fixture.TeamId)],
                testContext.CancellationToken);

            team.ShouldNotBeNull();
            team.IsArchived.ShouldBeFalse();
        });
    }
}
