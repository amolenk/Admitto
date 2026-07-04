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
    [TestMethod]
    public async ValueTask UpdateTeam_PartialUpdateWithCorrectVersion_UpdatesNameOnly()
    {
        // Arrange
        // SC-007: Given team "acme" at version N, when email unchanged but name updated
        // with the correct version, the name changes and version increments.
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

    [TestMethod]
    public async ValueTask UpdateTeam_StaleVersion_ThrowsConcurrencyConflict()
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
            Name: "Acme Corp",
            ExpectedVersion: wrongVersion);
        var sut = new UpdateTeamHandler(Environment.OrganizationDatabase.Context);

        // Act & Assert
        var exception = await Should.ThrowAsync<BusinessRuleViolationException>(
            async () => await sut.HandleAsync(command, testContext.CancellationToken));

        exception.Error.ShouldMatch(ConcurrencyConflictError.Create(wrongVersion, fixture.TeamVersion));
    }

    [TestMethod]
    public async ValueTask UpdateTeam_ArchivedTeam_ThrowsTeamArchived()
    {
        // Arrange
        // SC-010: Given team "acme" is archived, when an owner attempts to update the name,
        // the request is rejected because the team is archived.
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

    [TestMethod]
    public async ValueTask UpdateTeam_ReplyToEmailAddress_UpdatesReplyToEmailAddress()
    {
        var fixture = UpdateTeamFixture.ActiveTeam();
        await fixture.SetupAsync(Environment);

        var command = new UpdateTeamCommand(
            fixture.TeamId,
            Name: null,
            ExpectedVersion: fixture.TeamVersion,
            ReplyToEmailAddress: "help@example.com");

        var sut = new UpdateTeamHandler(Environment.OrganizationDatabase.Context);

        await sut.HandleAsync(command, testContext.CancellationToken);

        await Environment.OrganizationDatabase.AssertAsync(async dbContext =>
        {
            var team = await dbContext.Teams.FindAsync(
                [TeamId.From(fixture.TeamId)],
                testContext.CancellationToken);

            team.ShouldNotBeNull();
            team.ReplyToEmailAddress.ShouldBe(EmailAddress.From("help@example.com"));
        });
    }

    [TestMethod]
    public async ValueTask UpdateTeam_ClearReplyToEmailAddress_ClearsReplyToEmailAddress()
    {
        var fixture = UpdateTeamFixture.ActiveTeam(replyToEmailAddress: "help@example.com");
        await fixture.SetupAsync(Environment);

        var command = new UpdateTeamCommand(
            fixture.TeamId,
            Name: null,
            ExpectedVersion: fixture.TeamVersion,
            ClearReplyToEmailAddress: true);

        var sut = new UpdateTeamHandler(Environment.OrganizationDatabase.Context);

        await sut.HandleAsync(command, testContext.CancellationToken);

        await Environment.OrganizationDatabase.AssertAsync(async dbContext =>
        {
            var team = await dbContext.Teams.FindAsync(
                [TeamId.From(fixture.TeamId)],
                testContext.CancellationToken);

            team.ShouldNotBeNull();
            team.ReplyToEmailAddress.ShouldBeNull();
        });
    }
}
