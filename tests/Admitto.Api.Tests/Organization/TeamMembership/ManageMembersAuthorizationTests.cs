using System.Net;
using System.Net.Http.Json;
using Amolenk.Admitto.Api.Tests.Infrastructure;
using Amolenk.Admitto.Core.Organization.Contracts;
using Shouldly;

namespace Amolenk.Admitto.Api.Tests.Organization.TeamMembership;

[TestClass]
public sealed class ManageMembersAuthorizationTests(TestContext testContext) : EndToEndTestBase
{
    // Given the requester is a Crew member of the team
    // When they attempt to add a new member to that team
    // Then the API returns 403 Forbidden
    [TestMethod]
    public async Task CrewMember_CannotManageMembers_Returns403Forbidden()
    {
        // Arrange
        var fixture = ManageMembersAuthorizationFixture.BobIsCrewMember();
        await fixture.SetupAsync(Environment);

        var request = new { Email = "newmember@example.com", Role = TeamMembershipRoleDto.Crew };

        // Act
        var response = await Environment.BobApiClient.PostAsJsonAsync(
            fixture.MembersRoute,
            request,
            cancellationToken: testContext.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    // Given the requester is an owner of a different team, not the target team
    // When they attempt to add a new member to the target team
    // Then the API returns 403 Forbidden
    [TestMethod]
    public async Task OwnerOfDifferentTeam_CannotManageMembers_Returns403Forbidden()
    {
        // Arrange
        var fixture = ManageMembersAuthorizationFixture.BobIsOwnerOfDifferentTeam();
        await fixture.SetupWithOtherTeamMembershipAsync(Environment);

        var request = new { Email = "newmember@example.com", Role = TeamMembershipRoleDto.Crew };

        // Act
        var response = await Environment.BobApiClient.PostAsJsonAsync(
            fixture.MembersRoute,
            request,
            cancellationToken: testContext.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    // Given the requester is a platform admin who is not a member of the team
    // When they add a new Crew member to that team
    // Then the API returns 200 OK
    [TestMethod]
    public async Task Admin_BypassesOwnershipCheck_Returns200Ok()
    {
        // Arrange
        var fixture = ManageMembersAuthorizationFixture.NoTeamMembers();
        await fixture.SetupTeamOnlyAsync(Environment);

        var request = new { Email = "alice@example.com", Role = TeamMembershipRoleDto.Crew };

        // Act
        var response = await Environment.ApiClient.PostAsJsonAsync(
            fixture.MembersRoute,
            request,
            cancellationToken: testContext.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }
}
