using Amolenk.Admitto.Module.Organization.Tests.Application.Infrastructure;
using Amolenk.Admitto.Module.Organization.Application.UseCases.TeamManagement.CreateTeam;
using Amolenk.Admitto.Module.Shared.Kernel.ErrorHandling;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;
using Amolenk.Admitto.Testing.Infrastructure.Assertions;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Should = Shouldly.Should;

namespace Amolenk.Admitto.Module.Organization.Tests.Application.UseCases.TeamManagement.CreateTeam;

[TestClass]
public sealed class CreateTeamTests(TestContext testContext) : AspireIntegrationTestBase
{
    [TestMethod]
    public async ValueTask SC001_CreateTeam_ValidCommand_CreatesTeam()
    {
        // Arrange
        const string slug = "team-bravo";
        const string name = "Team Bravo";
        const string emailAddress = "team-bravo@example.com";
        var command = NewCreateTeamCommand(slug, name, emailAddress);
        var sut = NewCreateTeamHandler();

        // Act
        await sut.HandleAsync(command, testContext.CancellationToken);

        // Assert
        await Environment.Database.AssertAsync(async dbContext =>
        {
            // Verify that one team has been created with the expected values.
            var team = await dbContext.Teams.SingleOrDefaultAsync(testContext.CancellationToken);

            team.ShouldNotBeNull();
            team.Slug.Value.ShouldBe(command.Slug);
            team.Name.Value.ShouldBe(command.Name);
            team.EmailAddress.Value.ShouldBe(command.EmailAddress);
        });
    }

    [TestMethod]
    public async ValueTask SC002_CreateTeam_DuplicateSlug_ThrowsDbUpdateException()
    {
        // Arrange
        var fixture = CreateTeamFixture.DuplicateSlug();
        await fixture.SetupAsync(Environment);

        var command = NewCreateTeamCommand(
            fixture.TeamSlug,
            "Another Team",
            "another@example.com");
        var sut = NewCreateTeamHandler();

        // Act
        await sut.HandleAsync(command, testContext.CancellationToken);

        var exception = Should.Throw<DbUpdateException>(
            () => Environment.Database.Context.SaveChangesAsync(testContext.CancellationToken));

        // Assert
        exception.InnerException
            .ShouldBeAssignableTo<PostgresException>()?
            .ConstraintName.ShouldBe("IX_teams_slug");
    }

    private static CreateTeamCommand NewCreateTeamCommand(
        string? slug = null,
        string? name = null,
        string? emailAddress = null)
    {
        slug ??= "team-charlie";
        name ??= "Team Charlie";
        emailAddress ??= "team-charlie@example.com";

        return new CreateTeamCommand(slug, name, emailAddress);
    }

    private static CreateTeamHandler NewCreateTeamHandler() =>
        new(Environment.Database.Context);

    [TestMethod]
    public async ValueTask SC003_CreateTeam_EmptyName_ThrowsBusinessRuleViolation()
    {
        // Arrange
        var command = NewCreateTeamCommand(name: string.Empty);
        var sut = NewCreateTeamHandler();

        // Act & Assert
        // DisplayName.From("") throws BusinessRuleViolationException with code "text.empty"
        // because the value object enforces a non-empty, non-whitespace constraint.
        var exception = await Should.ThrowAsync<BusinessRuleViolationException>(
            async () => await sut.HandleAsync(command, testContext.CancellationToken));

        exception.Error.ShouldMatch(CommonErrors.TextEmpty);
    }
}
