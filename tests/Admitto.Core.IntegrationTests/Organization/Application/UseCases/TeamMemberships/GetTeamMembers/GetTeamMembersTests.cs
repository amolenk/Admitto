using Amolenk.Admitto.Core.Organization.Application.UseCases.TeamMemberships.GetTeamMembers;
using Amolenk.Admitto.Core.Organization.Contracts;

namespace Amolenk.Admitto.Core.IntegrationTests.Organization.Application.UseCases.TeamMemberships.GetTeamMembers;

[TestClass]
public sealed class GetTeamMembersTests(TestContext testContext) : AspireIntegrationTestBase
{
    [TestMethod]
    public async ValueTask GetTeamMembers_TeamHasMembers_ReturnsAllMembers()
    {
        // Arrange
        // SC-004: Given team has two members (alice = Owner, bob = Crew), when the team
        // members are listed, both members are returned with their correct roles.
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

    [TestMethod]
    public async ValueTask GetTeamMembers_TeamHasNoMembers_ReturnsEmptyList()
    {
        // Arrange
        // SC-005: Given an empty team (no members), when the team members are listed,
        // an empty list is returned.
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
