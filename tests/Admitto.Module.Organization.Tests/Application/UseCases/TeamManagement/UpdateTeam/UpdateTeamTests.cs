using Amolenk.Admitto.Module.Organization.Tests.Application.Infrastructure;
using Amolenk.Admitto.Module.Organization.Application.UseCases.TeamManagement.UpdateTeam;
using Amolenk.Admitto.Module.Organization.Domain.Entities;
using Amolenk.Admitto.Module.Shared.Kernel.ErrorHandling;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;
using Amolenk.Admitto.Testing.Infrastructure.Assertions;
using Microsoft.EntityFrameworkCore;
using Should = Shouldly.Should;

namespace Amolenk.Admitto.Module.Organization.Tests.Application.UseCases.TeamManagement.UpdateTeam;

[TestClass]
public sealed class UpdateTeamTests(TestContext testContext) : AspireIntegrationTestBase
{
    [TestMethod]
    public async ValueTask SC007_UpdateTeam_PartialUpdateWithCorrectVersion_UpdatesNameOnly()
    {
        // Arrange
        // SC-007: Given team "acme" at version N, when slug+email unchanged but name updated
        // with the correct version, the name changes and version increments.
        var fixture = UpdateTeamFixture.ActiveTeam();
        await fixture.SetupAsync(Environment);

        var command = new UpdateTeamCommand(
            fixture.TeamId,
            Slug: null,
            Name: "Acme Corp",
            EmailAddress: null,
            ExpectedVersion: fixture.TeamVersion);

        var sut = new UpdateTeamHandler(Environment.Database.Context);

        // Act
        await sut.HandleAsync(command, testContext.CancellationToken);

        // Assert
        await Environment.Database.AssertAsync(async dbContext =>
        {
            var team = await dbContext.Teams.FindAsync(
                [Amolenk.Admitto.Module.Shared.Kernel.ValueObjects.TeamId.From(fixture.TeamId)],
                testContext.CancellationToken);

            team.ShouldNotBeNull();
            team.Name.Value.ShouldBe("Acme Corp");
            team.Slug.Value.ShouldBe(fixture.OriginalSlug);
            team.EmailAddress.Value.ShouldBe(fixture.OriginalEmail);
            team.Version.ShouldBeGreaterThan(fixture.TeamVersion);
        });
    }

    [TestMethod]
    public async ValueTask SC008_UpdateTeam_StaleVersion_ThrowsConcurrencyConflict()
    {
        // Arrange
        // SC-008: Given team "acme" at version N, when update is submitted with version N-1,
        // the request is rejected with a concurrency conflict error.
        var fixture = UpdateTeamFixture.ActiveTeam();
        await fixture.SetupAsync(Environment);

        // Use a version that does not match the current version (use 0 if version > 0,
        // otherwise use max — in practice PostgreSQL xmin is always > 0 after a save).
        var wrongVersion = fixture.TeamVersion > 0 ? 0u : uint.MaxValue;

        var command = new UpdateTeamCommand(
            fixture.TeamId,
            Slug: null,
            Name: "Acme Corp",
            EmailAddress: null,
            ExpectedVersion: wrongVersion);
        var sut = new UpdateTeamHandler(Environment.Database.Context);

        // Act & Assert
        var exception = await Should.ThrowAsync<BusinessRuleViolationException>(
            async () => await sut.HandleAsync(command, testContext.CancellationToken));

        exception.Error.ShouldMatch(CommonErrors.ConcurrencyConflict(wrongVersion, fixture.TeamVersion));
    }

    [TestMethod]
    public async ValueTask SC010_UpdateTeam_ArchivedTeam_ThrowsTeamArchived()
    {
        // Arrange
        // SC-010: Given team "acme" is archived, when an owner attempts to update the name,
        // the request is rejected because the team is archived.
        var fixture = UpdateTeamFixture.ArchivedTeam();
        await fixture.SetupAsync(Environment);

        var command = new UpdateTeamCommand(
            fixture.TeamId,
            Slug: null,
            Name: "Acme Corp",
            EmailAddress: null,
            ExpectedVersion: fixture.TeamVersion);
        var sut = new UpdateTeamHandler(Environment.Database.Context);

        // Act & Assert
        var exception = await Should.ThrowAsync<BusinessRuleViolationException>(
            async () => await sut.HandleAsync(command, testContext.CancellationToken));

        exception.Error.ShouldMatch(Team.Errors.TeamArchived(TeamId.From(fixture.TeamId)));
    }
}
