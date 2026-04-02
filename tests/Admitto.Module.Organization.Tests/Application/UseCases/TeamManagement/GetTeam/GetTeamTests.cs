using Amolenk.Admitto.Module.Organization.Tests.Application.Infrastructure;
using Amolenk.Admitto.Module.Organization.Application.UseCases.TeamManagement.GetTeam;
using Should = Shouldly.Should;

namespace Amolenk.Admitto.Module.Organization.Tests.Application.UseCases.TeamManagement.GetTeam;

[TestClass]
public sealed class GetTeamTests(TestContext testContext) : AspireIntegrationTestBase
{
    [TestMethod]
    public async ValueTask SC004_GetTeam_TeamExists_ReturnsTeamDetails()
    {
        // Arrange
        // SC-004: Given a team "acme" exists, when a member requests it,
        // the team's slug, name, email address, and version are returned.
        var fixture = GetTeamFixture.TeamExists();
        await fixture.SetupAsync(Environment);

        var query = new GetTeamQuery(fixture.TeamId);
        var sut = new GetTeamHandler(Environment.Database.Context);

        // Act
        var result = await sut.HandleAsync(query, testContext.CancellationToken);

        // Assert
        result.ShouldNotBeNull();
        result.Slug.ShouldBe(fixture.TeamSlug);
        result.Name.ShouldBe(fixture.TeamName);
        result.EmailAddress.ShouldBe(fixture.TeamEmail);
        result.Version.ShouldBeGreaterThan(0u);
    }

    [TestMethod]
    [Ignore("SC-005: Authorization (non-member denied access) is enforced at the HTTP pipeline layer " +
            "via TeamMembershipAuthorizationHandler, not at the query handler level. " +
            "Covered by end-to-end tests in Admitto.Api.Tests.")]
    public async ValueTask SC005_GetTeam_NonMemberRequests_Unauthorized()
    {
        // This scenario verifies that a user who is not a member of the team cannot
        // view its details. The handler itself performs no authorization check —
        // it returns data for any caller. Authorization is applied as a policy on
        // the HTTP endpoint before the handler is invoked.
        await Task.CompletedTask;
    }
}
