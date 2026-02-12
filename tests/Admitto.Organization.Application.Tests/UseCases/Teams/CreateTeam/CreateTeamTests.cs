using Amolenk.Admitto.Organization.Application.Tests.Infrastructure;
using Amolenk.Admitto.Organization.Application.UseCases.Teams.CreateTeam;
using Amolenk.Admitto.Organization.Domain.Entities;
using Amolenk.Admitto.Shared.Kernel.ErrorHandling;
using Amolenk.Admitto.Shared.Kernel.ValueObjects;
using Amolenk.Admitto.Testing.Infrastructure.Assertions;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Should = Shouldly.Should;

namespace Amolenk.Admitto.Organization.Application.Tests.UseCases.Teams.CreateTeam;

[TestClass]
public sealed class CreateTeamTests(TestContext testContext) : AspireIntegrationTestBase
{
    [TestMethod]
    public async ValueTask CreateTeam_ValidCommand_CreatesTeam()
    {
        // Arrange
        var slug = Slug.From("team-bravo");
        var name = DisplayName.From("Team Bravo");
        var emailAddress = EmailAddress.From("team-bravo@example.com");
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
            team.Slug.ShouldBe(command.Slug);
            team.Name.ShouldBe(command.Name);
            team.EmailAddress.ShouldBe(command.EmailAddress);
        });
    }

    [TestMethod]
    public async ValueTask CreateTeam_DuplicateSlug_ThrowsDbUpdateException()
    {
        // Arrange
        var fixture = CreateTeamFixture.DuplicateSlug();
        await fixture.SetupAsync(Environment);

        var command = NewCreateTeamCommand(
            fixture.Slug,
            DisplayName.From("Another Team"),
            EmailAddress.From("another@example.com"));
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
        Slug? slug = null,
        DisplayName? name = null,
        EmailAddress? emailAddress = null)
    {
        slug ??= Slug.From("team-charlie");
        name ??= DisplayName.From("Team Charlie");
        emailAddress ??= EmailAddress.From("team-charlie@example.com");

        return new CreateTeamCommand(slug.Value, name.Value, emailAddress.Value);
    }

    private static CreateTeamHandler NewCreateTeamHandler() =>
        new(Environment.Database.Context);
}
