using Amolenk.Admitto.Core.Organization.Application.UseCases.Teams.UpdateTeam;
using Amolenk.Admitto.Core.Organization.Domain.Entities;
using Amolenk.Admitto.Core.Shared.Kernel.ErrorHandling;
using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;
using Amolenk.Admitto.Testing.Infrastructure.Assertions;
using Should = Shouldly.Should;

namespace Amolenk.Admitto.Core.IntegrationTests.Organization.Application.UseCases.Teams.UpdateTeam;

[TestClass]
public sealed class UpdateTeamTests(TestContext testContext) : AspireIntegrationTestBase
{
    // Given an active team at a known version
    // When the name is updated with the correct expected version
    // Then the name is changed and the version is incremented
    [TestMethod]
    public async ValueTask UpdateTeam_PartialUpdateWithCorrectVersion_UpdatesNameOnly()
    {
        // Arrange
        var fixture = UpdateTeamFixture.ActiveTeam();
        await fixture.SetupAsync(Environment);

        var command = new UpdateTeamCommand(
            fixture.TeamId,
            Name: "Acme Corp",
            ExpectedVersion: fixture.TeamVersion);

        var sut = new UpdateTeamHandler(Environment.OrganizationDatabase.Context);

        // Act
        await sut.HandleAsync(command, testContext.CancellationToken);

        // Assert
        await Environment.OrganizationDatabase.AssertAsync(async dbContext =>
        {
            var team = await dbContext.Teams.FindAsync(
                [Amolenk.Admitto.Core.Shared.Kernel.ValueObjects.TeamId.From(fixture.TeamId)],
                testContext.CancellationToken);

            team.ShouldNotBeNull();
            team.Name.Value.ShouldBe("Acme Corp");
            team.Version.ShouldBeGreaterThan(fixture.TeamVersion);
        });
    }

    // Given an active team at a known version
    // When the update is submitted with a stale, non-matching expected version
    // Then a concurrency conflict error is thrown
    [TestMethod]
    public async ValueTask UpdateTeam_StaleVersion_ThrowsConcurrencyConflict()
    {
        // Arrange
        var fixture = UpdateTeamFixture.ActiveTeam();
        await fixture.SetupAsync(Environment);

        // Use a version that does not match the current version (use 0 if version > 0,
        // otherwise use max — in practice PostgreSQL xmin is always > 0 after a save).
        var wrongVersion = fixture.TeamVersion > 0 ? 0u : uint.MaxValue;

        var command = new UpdateTeamCommand(
            fixture.TeamId,
            Name: "Acme Corp",
            ExpectedVersion: wrongVersion);
        var sut = new UpdateTeamHandler(Environment.OrganizationDatabase.Context);

        // Act & Assert
        var exception = await Should.ThrowAsync<BusinessRuleViolationException>(
            async () => await sut.HandleAsync(command, testContext.CancellationToken));

        exception.Error.ShouldMatch(ConcurrencyConflictError.Create(wrongVersion, fixture.TeamVersion));
    }

    // Given an archived team
    // When an update to the team's name is attempted
    // Then a team-archived error is thrown
    [TestMethod]
    public async ValueTask UpdateTeam_ArchivedTeam_ThrowsTeamArchived()
    {
        // Arrange
        var fixture = UpdateTeamFixture.ArchivedTeam();
        await fixture.SetupAsync(Environment);

        var command = new UpdateTeamCommand(
            fixture.TeamId,
            Name: "Acme Corp",
            ExpectedVersion: fixture.TeamVersion);
        var sut = new UpdateTeamHandler(Environment.OrganizationDatabase.Context);

        // Act & Assert
        var exception = await Should.ThrowAsync<BusinessRuleViolationException>(
            async () => await sut.HandleAsync(command, testContext.CancellationToken));

        exception.Error.ShouldMatch(Team.Errors.TeamArchived(TeamId.From(fixture.TeamId)));
    }
}
