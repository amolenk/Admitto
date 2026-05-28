using Amolenk.Admitto.Core.Organization.Application.UseCases.Teams.CreateTeam;
using Amolenk.Admitto.Core.Shared.Kernel.ErrorHandling;
using Amolenk.Admitto.Testing.Infrastructure.Assertions;
using Microsoft.EntityFrameworkCore;
using Should = Shouldly.Should;

namespace Amolenk.Admitto.Core.IntegrationTests.Organization.Application.UseCases.Teams.CreateTeam;

[TestClass]
public sealed class CreateTeamTests(TestContext testContext) : AspireIntegrationTestBase
{
    [TestMethod]
    public async ValueTask CreateTeam_ValidCommand_CreatesTeam()
    {
        // Arrange
        const string name = "Team Bravo";
        var command = NewCreateTeamCommand(name);
        var sut = NewCreateTeamHandler();

        // Act
        await sut.HandleAsync(command, testContext.CancellationToken);

        // Assert
        await Environment.OrganizationDatabase.AssertAsync(async dbContext =>
        {
            // Verify that one team has been created with the expected values.
            var team = await dbContext.Teams.SingleOrDefaultAsync(testContext.CancellationToken);

            team.ShouldNotBeNull();
            team.Name.Value.ShouldBe(command.Name);
        });
    }

    private static CreateTeamCommand NewCreateTeamCommand(
        string? name = null)
    {
        name ??= "Team Charlie";

        return new CreateTeamCommand(name);
    }

    private static CreateTeamHandler NewCreateTeamHandler() =>
        new(Environment.OrganizationDatabase.Context);

    [TestMethod]
    public async ValueTask CreateTeam_EmptyName_ThrowsBusinessRuleViolation()
    {
        // Arrange
        var command = NewCreateTeamCommand(name: string.Empty);
        var sut = NewCreateTeamHandler();

        // Act & Assert
        // EventName.From("") throws BusinessRuleViolationException with code "text.empty"
        // because the value object enforces a non-empty, non-whitespace constraint.
        var exception = await Should.ThrowAsync<BusinessRuleViolationException>(
            async () => await sut.HandleAsync(command, testContext.CancellationToken));

        exception.Error.ShouldMatch(CommonErrors.TextEmpty);
    }
}
