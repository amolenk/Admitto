using System.Net;
using Amolenk.Admitto.Api.Tests.Infrastructure;
using Shouldly;

namespace Amolenk.Admitto.Api.Tests.Organization.TeamMembership;

[TestClass]
public sealed class ResendTeamMemberInviteAuthorizationTests(TestContext testContext) : EndToEndTestBase
{
    // Given the requester is a Crew member of the team
    // When they attempt to resend another member's invite
    // Then the API returns 403 Forbidden
    [TestMethod]
    public async Task CrewMember_CannotResendInvite_Returns403Forbidden()
    {
        // Arrange
        var fixture = ResendTeamMemberInviteAuthorizationFixture.BobIsCrewMember();
        await fixture.SetupAsync(Environment);

        // Act
        var response = await Environment.BobApiClient.PostAsync(
            fixture.ResendInviteRoute,
            content: null,
            cancellationToken: testContext.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    // Given the requester is an owner of a different team, not the target team
    // When they attempt to resend a member's invite on the target team
    // Then the API returns 403 Forbidden
    [TestMethod]
    public async Task OwnerOfDifferentTeam_CannotResendInvite_Returns403Forbidden()
    {
        // Arrange
        var fixture = ResendTeamMemberInviteAuthorizationFixture.BobIsOwnerOfDifferentTeam();
        await fixture.SetupWithOtherTeamMembershipAsync(Environment);

        // Act
        var response = await Environment.BobApiClient.PostAsync(
            fixture.ResendInviteRoute,
            content: null,
            cancellationToken: testContext.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    // Given the requester is an owner of the target team
    // When they resend a Crew member's invite
    // Then the API returns 200 OK
    [TestMethod]
    public async Task Owner_CanResendInvite_Returns200Ok()
    {
        // Arrange
        var fixture = ResendTeamMemberInviteAuthorizationFixture.BobIsOwner();
        await fixture.SetupWithBobAsOwnerAsync(Environment);

        // Act
        var response = await Environment.BobApiClient.PostAsync(
            fixture.ResendInviteRoute,
            content: null,
            cancellationToken: testContext.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }
}
