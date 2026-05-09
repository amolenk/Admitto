using Amolenk.Admitto.Core.Module.Organization.Tests.Application.Infrastructure;
using Amolenk.Admitto.Core.Module.Organization.Application.UseCases.TeamManagement.CreateTeam;
using Amolenk.Admitto.Core.Shared.Kernel.ErrorHandling;
using Amolenk.Admitto.Testing.Infrastructure.Assertions;
using Microsoft.EntityFrameworkCore;
using Should = Shouldly.Should;

namespace Amolenk.Admitto.Core.Module.Organization.Tests.Application.UseCases.TeamManagement.CreateTeam;

[TestClass]
public sealed class CreateTeamTests(TestContext testContext) : AspireIntegrationTestBase
{
    [TestMethod]
    public async ValueTask SC001_CreateTeam_ValidCommand_CreatesTeam()
    {
        // Arrange
        const string name = "Team Bravo";
        const string emailAddress = "team-bravo@example.com";
        var command = NewCreateTeamCommand(name, emailAddress);
        var sut = NewCreateTeamHandler();

        // Act
        await sut.HandleAsync(command, testContext.CancellationToken);

        // Assert
        await Environment.Database.AssertAsync(async dbContext =>
        {
            // Verify that one team has been created with the expected values.
            var team = await dbContext.Teams.SingleOrDefaultAsync(testContext.CancellationToken);

            team.ShouldNotBeNull();
            team.Name.Value.ShouldBe(command.Name);
            team.EmailAddress.Value.ShouldBe(command.EmailAddress);
        });
    }

    private static CreateTeamCommand NewCreateTeamCommand(
        string? name = null,
        string? emailAddress = null)
    {
        name ??= "Team Charlie";
        emailAddress ??= "team-charlie@example.com";

        return new CreateTeamCommand(name, emailAddress);
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
