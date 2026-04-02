using Amolenk.Admitto.Module.Organization.Tests.Application.Infrastructure;
using Amolenk.Admitto.Module.Organization.Application.UseCases.TeamMembershipManagement.ListTeamMembers;
using Amolenk.Admitto.Module.Organization.Contracts;

namespace Amolenk.Admitto.Module.Organization.Tests.Application.UseCases.TeamMembershipManagement.ListTeamMembers;

[TestClass]
public sealed class ListTeamMembersTests(TestContext testContext) : AspireIntegrationTestBase
{
    [TestMethod]
    public async ValueTask SC004_ListTeamMembers_TeamHasMembers_ReturnsAllMembers()
    {
        // Arrange
        // SC-004: Given team has two members (alice = Owner, bob = Crew), when the team
        // members are listed, both members are returned with their correct roles.
        var fixture = ListTeamMembersFixture.TeamWithMembers();
        await fixture.SetupAsync(Environment);

        var query = new GetTeamMembersQuery(fixture.TeamId);
        var sut = new GetTeamMembersHandler(Environment.Database.Context);

        // Act
        var result = await sut.HandleAsync(query, testContext.CancellationToken);

        // Assert
        result.ShouldNotBeNull();
        result.Count.ShouldBe(2);
        result.ShouldContain(m => m.Email == "alice@example.com" && m.Role == TeamMembershipRoleDto.Owner);
        result.ShouldContain(m => m.Email == "bob@example.com" && m.Role == TeamMembershipRoleDto.Crew);
    }

    [TestMethod]
    public async ValueTask SC005_ListTeamMembers_TeamHasNoMembers_ReturnsEmptyList()
    {
        // Arrange
        // SC-005: Given an empty team (no members), when the team members are listed,
        // an empty list is returned.
        var fixture = ListTeamMembersFixture.EmptyTeam();
        await fixture.SetupAsync(Environment);

        var query = new GetTeamMembersQuery(fixture.TeamId);
        var sut = new GetTeamMembersHandler(Environment.Database.Context);

        // Act
        var result = await sut.HandleAsync(query, testContext.CancellationToken);

        // Assert
        result.ShouldNotBeNull();
        result.ShouldBeEmpty();
    }
}
