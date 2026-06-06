using System.Security.Claims;
using Amolenk.Admitto.Core.Organization.Domain.ValueObjects;
using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;
using ExternalUserIdVO = Amolenk.Admitto.Core.Organization.Domain.ValueObjects.ExternalUserId;

namespace Amolenk.Admitto.Core.IntegrationTests.Organization.Auth;

[TestClass]
public sealed class UserContextResolverTests : AspireIntegrationTestBase
{
    [TestMethod]
    public async Task FirstSignIn_BindsExternalUserIdAndReturnsContext()
    {
        // Arrange
        // On first sign-in the resolver finds the pre-invited user by email,
        // stores the sub claim as ExternalUserId, and returns a populated UserContextDto.
        var fixture = new UserContextResolverFixture();
        await fixture.SeedUserWithoutExternalIdAsync(Environment);

        var principal = BuildPrincipal(
            sub: UserContextResolverFixture.ExternalUserId,
            email: UserContextResolverFixture.UserEmail,
            name: UserContextResolverFixture.DisplayName);

        var sut = fixture.CreateResolver(Environment);

        // Act
        var result = await sut.ResolveAsync(principal, null, null, CancellationToken.None);

        // Assert
        result.ShouldNotBeNull();
        result.UserId.ShouldBe(fixture.UserId);

        // ExternalUserId should now be persisted
        await Environment.OrganizationDatabase.AssertAsync(async dbContext =>
        {
            var user = await dbContext.Users.FindAsync([UserId.From(fixture.UserId)]);
            user.ShouldNotBeNull();
            user.ExternalUserId.ShouldBe(ExternalUserIdVO.From(UserContextResolverFixture.ExternalUserId));
        });
    }

    [TestMethod]
    public async Task SubsequentSignIn_ResolvesDirectlyByExternalUserId()
    {
        // Arrange
        // After the ExternalUserId is bound, subsequent requests resolve
        // by the sub claim directly without touching the email lookup path.
        var fixture = new UserContextResolverFixture();
        await fixture.SeedUserWithExternalIdAsync(Environment);

        var principal = BuildPrincipal(
            sub: UserContextResolverFixture.ExternalUserId,
            email: UserContextResolverFixture.UserEmail,
            name: UserContextResolverFixture.DisplayName);

        var sut = fixture.CreateResolver(Environment);

        // Act
        var result = await sut.ResolveAsync(principal, null, null, CancellationToken.None);

        // Assert
        result.ShouldNotBeNull();
        result.UserId.ShouldBe(fixture.UserId);
    }

    [TestMethod]
    public async Task ExternalUserIdMismatch_ReturnsNull()
    {
        // Arrange
        // If a user already has a different ExternalUserId stored,
        // the resolver must reject the request (returns null → caller gets 403).
        var fixture = new UserContextResolverFixture();
        await fixture.SeedUserWithExternalIdAsync(Environment); // seeded with ExternalUserId="auth0|abc123"

        var principal = BuildPrincipal(
            sub: "auth0|differentSub",
            email: UserContextResolverFixture.UserEmail,
            name: UserContextResolverFixture.DisplayName);

        var sut = fixture.CreateResolver(Environment);

        // Act
        var result = await sut.ResolveAsync(principal, null, null, CancellationToken.None);

        // Assert
        result.ShouldBeNull();
    }

    [TestMethod]
    public async Task UnknownUser_ReturnsNull()
    {
        // Arrange
        // A sub/email combination that doesn't match any known user returns null → 403.
        var fixture = new UserContextResolverFixture();
        // No user seeded

        var principal = BuildPrincipal(
            sub: "auth0|unknownUser",
            email: "unknown@example.com",
            name: "Unknown");

        var sut = fixture.CreateResolver(Environment);

        // Act
        var result = await sut.ResolveAsync(principal, null, null, CancellationToken.None);

        // Assert
        result.ShouldBeNull();
    }

    [TestMethod]
    public async Task TeamContext_UserIsMember_RolePopulated()
    {
        // Arrange
        // When a teamId is present in the route and the user is a member,
        // the resolved context carries the correct role.
        var fixture = new UserContextResolverFixture();
        await fixture.SeedUserWithTeamMembershipAsync(Environment, TeamMembershipRole.Organizer);

        var principal = BuildPrincipal(
            sub: UserContextResolverFixture.ExternalUserId,
            email: UserContextResolverFixture.UserEmail,
            name: UserContextResolverFixture.DisplayName);

        var sut = fixture.CreateResolver(Environment);
        var teamId = TeamId.From(fixture.TeamId);

        // Act
        var result = await sut.ResolveAsync(principal, teamId, null, CancellationToken.None);

        // Assert
        result.ShouldNotBeNull();
        result.UserId.ShouldBe(fixture.UserId);
        result.Role.ShouldBe(TeamMembershipRole.Organizer);
    }

    [TestMethod]
    public async Task AdminUser_NoMemberships_StillResolves()
    {
        // Arrange
        // An admin with no team memberships must still resolve
        // successfully — admins are not gated by membership.
        var fixture = new UserContextResolverFixture();
        await fixture.SeedAdminWithoutMembershipsAsync(Environment);

        var principal = BuildPrincipal(
            sub: UserContextResolverFixture.ExternalUserId,
            email: UserContextResolverFixture.UserEmail,
            name: UserContextResolverFixture.DisplayName);

        var sut = fixture.CreateResolver(Environment);

        // Act
        var result = await sut.ResolveAsync(principal, null, null, CancellationToken.None);

        // Assert
        result.ShouldNotBeNull();
        result.UserId.ShouldBe(fixture.UserId);
        result.IsAdmin.ShouldBeTrue();
    }

    [TestMethod]
    public async Task EventContext_EventBelongsToTeam_Resolves()
    {
        // Arrange
        // When both teamId and eventId are present and the event belongs
        // to the team, the request resolves normally.
        var fixture = new UserContextResolverFixture();
        await fixture.SeedUserWithTeamAndEventAsync(Environment);

        var principal = BuildPrincipal(
            sub: UserContextResolverFixture.ExternalUserId,
            email: UserContextResolverFixture.UserEmail,
            name: UserContextResolverFixture.DisplayName);

        var sut = fixture.CreateResolver(Environment);
        var teamId = TeamId.From(fixture.TeamId);
        var eventId = TicketedEventId.From(fixture.EventId);

        // Act
        var result = await sut.ResolveAsync(principal, teamId, eventId, CancellationToken.None);

        // Assert
        result.ShouldNotBeNull();
        result.UserId.ShouldBe(fixture.UserId);
    }

    [TestMethod]
    public async Task EventContext_EventDoesNotBelongToTeam_ReturnsNull()
    {
        // Arrange
        // When a valid eventId is provided that does not belong to the
        // given teamId, the resolver must reject the request → 403.
        // This guards against users guessing eventIds from other teams.
        var fixture = new UserContextResolverFixture();
        await fixture.SeedUserWithTeamMembershipAsync(Environment);

        var principal = BuildPrincipal(
            sub: UserContextResolverFixture.ExternalUserId,
            email: UserContextResolverFixture.UserEmail,
            name: UserContextResolverFixture.DisplayName);

        var sut = fixture.CreateResolver(Environment);
        var teamId = TeamId.From(fixture.TeamId);
        var foreignEventId = TicketedEventId.New(); // not registered under this team

        // Act
        var result = await sut.ResolveAsync(principal, teamId, foreignEventId, CancellationToken.None);

        // Assert
        result.ShouldBeNull();
    }

    [TestMethod]
    public async Task AdminUser_EventDoesNotBelongToTeam_Resolves()
    {
        // Arrange
        // Admins bypass the event-scope guard — they can access any
        // event regardless of whether it's registered under the route's teamId.
        var fixture = new UserContextResolverFixture();
        await fixture.SeedAdminWithoutMembershipsAsync(Environment);

        var principal = BuildPrincipal(
            sub: UserContextResolverFixture.ExternalUserId,
            email: UserContextResolverFixture.UserEmail,
            name: UserContextResolverFixture.DisplayName);

        var sut = fixture.CreateResolver(Environment);
        var teamId = TeamId.New();
        var foreignEventId = TicketedEventId.New();

        // Act
        var result = await sut.ResolveAsync(principal, teamId, foreignEventId, CancellationToken.None);

        // Assert
        result.ShouldNotBeNull();
        result.IsAdmin.ShouldBeTrue();
    }

    private static ClaimsPrincipal BuildPrincipal(string sub, string email, string name)
        => new(new ClaimsIdentity(
        [
            new Claim(ClaimTypes.NameIdentifier, sub),
            new Claim(ClaimTypes.Email, email),
            new Claim("name", name),
        ]));
}
