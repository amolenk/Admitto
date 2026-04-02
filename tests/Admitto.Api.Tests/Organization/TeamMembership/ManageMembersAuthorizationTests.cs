using System.Net;
using System.Net.Http.Json;
using Amolenk.Admitto.Api.Tests.Infrastructure;
using Amolenk.Admitto.Module.Organization.Contracts;
using Shouldly;

namespace Amolenk.Admitto.Api.Tests.Organization.TeamMembership;

[TestClass]
public sealed class ManageMembersAuthorizationTests(TestContext testContext) : EndToEndTestBase
{
    // SC-014: Given the requester is a Crew member of team "acme",
    //         when they attempt to add a new member to team "acme",
    //         then the request is rejected as unauthorized.
    [TestMethod]
    public async Task SC014_CrewMember_CannotManageMembers_Returns403Forbidden()
    {
        // Arrange
        var fixture = ManageMembersAuthorizationFixture.BobIsCrewMember();
        await fixture.SetupAsync(Environment);

        var request = new { Email = "newmember@example.com", Role = TeamMembershipRoleDto.Crew };

        // Act
        var response = await Environment.BobApiClient.PostAsJsonAsync(
            ManageMembersAuthorizationFixture.MembersRoute,
            request,
            cancellationToken: testContext.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    // SC-015: Given the requester is an admin but not a member of team "acme",
    //         when they add "alice@example.com" as a Crew member of team "acme",
    //         then "alice@example.com" has a Crew membership in team "acme".
    [TestMethod]
    public async Task SC015_Admin_BypassesOwnershipCheck_Returns200Ok()
    {
        // Arrange
        var fixture = ManageMembersAuthorizationFixture.NoTeamMembers();
        await fixture.SetupTeamOnlyAsync(Environment);

        var request = new { Email = "alice@example.com", Role = TeamMembershipRoleDto.Crew };

        // Act
        var response = await Environment.ApiClient.PostAsJsonAsync(
            ManageMembersAuthorizationFixture.MembersRoute,
            request,
            cancellationToken: testContext.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }
}
