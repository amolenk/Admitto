using System.Security.Claims;
using Amolenk.Admitto.Api.Auth;
using Amolenk.Admitto.Core.Organization.Domain.ValueObjects;
using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;
using ExternalUserIdVO = Amolenk.Admitto.Core.Organization.Domain.ValueObjects.ExternalUserId;

namespace Amolenk.Admitto.Core.IntegrationTests.Organization.Auth;

[TestClass]
public sealed class UserContextResolverTests : AspireIntegrationTestBase
{
    // Given a pre-invited user with no ExternalUserId bound yet
    // When the principal signs in for the first time
    // Then the context resolves and the sub claim is persisted as the user's ExternalUserId
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
        var result = await sut.ResolveAsync(principal, new RouteScope.Global(), CancellationToken.None);

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

    // Given a user with an ExternalUserId already bound
    // When the principal signs in again with the same sub claim
    // Then the context resolves directly by ExternalUserId with no role
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
        var result = await sut.ResolveAsync(principal, new RouteScope.Global(), CancellationToken.None);

        // Assert
        result.ShouldNotBeNull();
        result.UserId.ShouldBe(fixture.UserId);
        result.Role.ShouldBeNull();
    }

    // Given a user with a different ExternalUserId already bound
    // When the principal signs in with a mismatching sub claim
    // Then resolution returns null
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
        var result = await sut.ResolveAsync(principal, new RouteScope.Global(), CancellationToken.None);

        // Assert
        result.ShouldBeNull();
    }

    // Given no user has been seeded
    // When the principal signs in with an unknown sub/email combination
    // Then resolution returns null
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
        var result = await sut.ResolveAsync(principal, new RouteScope.Global(), CancellationToken.None);

        // Assert
        result.ShouldBeNull();
    }

    // Given a user who is a member of the team with a specific role
    // When resolving in the scope of that team
    // Then the resolved context carries the user's role in that team
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
        var result = await sut.ResolveAsync(principal, new RouteScope.Team(teamId), CancellationToken.None);

        // Assert
        result.ShouldNotBeNull();
        result.UserId.ShouldBe(fixture.UserId);
        result.Role.ShouldBe(TeamMembershipRole.Organizer);
    }

    // Given a user who is a member of a different team than the requested one
    // When resolving in the scope of the requested team
    // Then the context resolves but the role is not populated
    [TestMethod]
    public async Task TeamContext_UserIsMemberOfDifferentTeam_RoleIsNotPopulated()
    {
        // Arrange
        var fixture = new UserContextResolverFixture();
        await fixture.SeedUserWithMembershipInOtherTeamAsync(Environment, TeamMembershipRole.Owner);

        var principal = BuildPrincipal(
            sub: UserContextResolverFixture.ExternalUserId,
            email: UserContextResolverFixture.UserEmail,
            name: UserContextResolverFixture.DisplayName);

        var sut = fixture.CreateResolver(Environment);
        var requestedTeamId = TeamId.From(fixture.TeamId);

        // Act
        var result = await sut.ResolveAsync(
            principal,
            new RouteScope.Team(requestedTeamId),
            CancellationToken.None);

        // Assert
        result.ShouldNotBeNull();
        result.UserId.ShouldBe(fixture.UserId);
        result.Role.ShouldBeNull();
    }

    // Given an admin user with no team memberships
    // When resolving in the global scope
    // Then the context resolves and marks the user as an admin
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
        var result = await sut.ResolveAsync(principal, new RouteScope.Global(), CancellationToken.None);

        // Assert
        result.ShouldNotBeNull();
        result.UserId.ShouldBe(fixture.UserId);
        result.IsAdmin.ShouldBeTrue();
    }

    // Given a user with a team membership and an event that belongs to that team
    // When resolving in the scope of that team and event
    // Then the context resolves with the user's role
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
        var result = await sut.ResolveAsync(
            principal,
            new RouteScope.Event(teamId, eventId),
            CancellationToken.None);

        // Assert
        result.ShouldNotBeNull();
        result.UserId.ShouldBe(fixture.UserId);
        result.Role.ShouldBe(TeamMembershipRole.Crew);
    }

    // Given a user with a team membership and an event that belongs to a different team
    // When resolving with a route scope pairing the user's team with that foreign event
    // Then resolution returns null
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
        var result = await sut.ResolveAsync(
            principal,
            new RouteScope.Event(teamId, foreignEventId),
            CancellationToken.None);

        // Assert
        result.ShouldBeNull();
    }

    // Given an admin user with no team memberships
    // When resolving with an event that does not belong to the requested team
    // Then the context still resolves because admins bypass the event-scope guard
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
        var result = await sut.ResolveAsync(
            principal,
            new RouteScope.Event(teamId, foreignEventId),
            CancellationToken.None);

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
