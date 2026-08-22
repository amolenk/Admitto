using Amolenk.Admitto.Core.Organization.Application.UseCases.Teams.GetTeam;

namespace Amolenk.Admitto.Core.IntegrationTests.Organization.Application.UseCases.Teams.GetTeam;

[TestClass]
public sealed class GetTeamTests(TestContext testContext) : AspireIntegrationTestBase
{
    // Given a team that exists
    // When its details are queried by id
    // Then the team's name and version are returned
    [TestMethod]
    public async ValueTask GetTeam_TeamExists_ReturnsTeamDetails()
    {
        // Arrange
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
