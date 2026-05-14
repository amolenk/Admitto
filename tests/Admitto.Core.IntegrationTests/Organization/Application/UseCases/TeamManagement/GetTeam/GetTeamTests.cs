using Amolenk.Admitto.Core.Organization.Application.UseCases.TeamManagement.GetTeam;

namespace Amolenk.Admitto.Core.IntegrationTests.Organization.Application.UseCases.TeamManagement.GetTeam;

[TestClass]
public sealed class GetTeamTests(TestContext testContext) : AspireIntegrationTestBase
{
    [TestMethod]
    public async ValueTask GetTeam_TeamExists_ReturnsTeamDetails()
    {
        // Arrange
        // SC-004: Given a team "acme" exists, when a member requests it,
        // the team's slug, name, and version are returned.
        var fixture = GetTeamFixture.TeamExists();
        await fixture.SetupAsync(Environment);

        var query = new GetTeamQuery(fixture.TeamId);
        var sut = new GetTeamHandler(Environment.OrganizationDatabase.Context);

        // Act
        var result = await sut.HandleAsync(query, testContext.CancellationToken);

        // Assert
        result.ShouldNotBeNull();
        result.Name.ShouldBe(fixture.TeamName);
        result.Version.ShouldBeGreaterThan(0u);
    }
}
