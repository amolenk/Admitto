using Amolenk.Admitto.Core.Organization.Application.UseCases.TeamMemberships.GetTeamMembers;
using Amolenk.Admitto.Core.Organization.Contracts;

namespace Amolenk.Admitto.Core.IntegrationTests.Organization.Application.UseCases.TeamMemberships.GetTeamMembers;

[TestClass]
public sealed class GetTeamMembersTests(TestContext testContext) : AspireIntegrationTestBase
{
    // Given a team with two members, alice (Owner) and bob (Crew)
    // When the team members are listed
    // Then both members are returned with their correct roles
    [TestMethod]
    public async ValueTask GetTeamMembers_TeamHasMembers_ReturnsAllMembers()
    {
        // Arrange
        var fixture = GetTeamMembersFixture.TeamWithMembers();
        await fixture.SetupAsync(Environment);

        var query = new GetTeamMembersQuery(fixture.TeamId);
        var sut = new GetTeamMembersHandler(Environment.OrganizationDatabase.Context);

        // Act
        var result = await sut.HandleAsync(query, testContext.CancellationToken);

        // Assert
        result.ShouldNotBeNull();
        result.Count.ShouldBe(2);
        result.ShouldContain(m => m.Email == "alice@example.com" && m.Role == TeamMembershipRoleDto.Owner);
        result.ShouldContain(m => m.Email == "bob@example.com" && m.Role == TeamMembershipRoleDto.Crew);
    }

    // Given a team with no members
    // When the team members are listed
    // Then an empty list is returned
    [TestMethod]
    public async ValueTask GetTeamMembers_TeamHasNoMembers_ReturnsEmptyList()
    {
        // Arrange
        var fixture = GetTeamMembersFixture.EmptyTeam();
        await fixture.SetupAsync(Environment);

        var query = new GetTeamMembersQuery(fixture.TeamId);
        var sut = new GetTeamMembersHandler(Environment.OrganizationDatabase.Context);

        // Act
        var result = await sut.HandleAsync(query, testContext.CancellationToken);

        // Assert
        result.ShouldNotBeNull();
        result.ShouldBeEmpty();
    }
}
