using System.Security.Claims;
using Amolenk.Admitto.Core.Organization.Domain.ValueObjects;
using ExternalUserIdVO = Amolenk.Admitto.Core.Organization.Domain.ValueObjects.ExternalUserId;

namespace Amolenk.Admitto.Core.IntegrationTests.Organization.Auth;

[TestClass]
public sealed class UserContextResolverTests : AspireIntegrationTestBase
{
    [TestMethod]
    public async Task FirstSignIn_BindsExternalUserIdAndReturnsContext()
    {
        // Arrange
        // SC-BIND: On first sign-in the resolver finds the pre-invited user by email,
        // stores the sub claim as ExternalUserId, and returns a populated UserContextDto.
        var fixture = new UserContextResolverFixture();
        await fixture.SeedUserWithoutExternalIdAsync(Environment);

        var principal = BuildPrincipal(
            sub: UserContextResolverFixture.ExternalUserId,
            email: UserContextResolverFixture.UserEmail,
            name: UserContextResolverFixture.DisplayName);

        var sut = fixture.CreateResolver(Environment);

        // Act
        var result = await sut.ResolveAsync(principal, CancellationToken.None);

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
        // SC-RESOLVE-BY-ID: After the ExternalUserId is bound, subsequent requests resolve
        // by the sub claim directly without touching the email lookup path.
        var fixture = new UserContextResolverFixture();
        await fixture.SeedUserWithExternalIdAsync(Environment);

        var principal = BuildPrincipal(
            sub: UserContextResolverFixture.ExternalUserId,
            email: UserContextResolverFixture.UserEmail,
            name: UserContextResolverFixture.DisplayName);

        var sut = fixture.CreateResolver(Environment);

        // Act
        var result = await sut.ResolveAsync(principal, CancellationToken.None);

        // Assert
        result.ShouldNotBeNull();
        result.UserId.ShouldBe(fixture.UserId);
    }

    [TestMethod]
    public async Task ExternalUserIdMismatch_ReturnsNull()
    {
        // Arrange
        // SC-MISMATCH-STORED-ID: If a user already has a different ExternalUserId stored,
        // the resolver must reject the request (returns null → caller gets 403).
        var fixture = new UserContextResolverFixture();
        await fixture.SeedUserWithExternalIdAsync(Environment); // seeded with ExternalUserId="auth0|abc123"

        var principal = BuildPrincipal(
            sub: "auth0|differentSub",
            email: UserContextResolverFixture.UserEmail,
            name: UserContextResolverFixture.DisplayName);

        var sut = fixture.CreateResolver(Environment);

        // Act
        var result = await sut.ResolveAsync(principal, CancellationToken.None);

        // Assert
        result.ShouldBeNull();
    }

    [TestMethod]
    public async Task UnknownUser_ReturnsNull()
    {
        // Arrange
        // SC-UNKNOWN: A sub/email combination that doesn't match any known user returns null → 403.
        var fixture = new UserContextResolverFixture();
        // No user seeded

        var principal = BuildPrincipal(
            sub: "auth0|unknownUser",
            email: "unknown@example.com",
            name: "Unknown");

        var sut = fixture.CreateResolver(Environment);

        // Act
        var result = await sut.ResolveAsync(principal, CancellationToken.None);

        // Assert
        result.ShouldBeNull();
    }

    private static ClaimsPrincipal BuildPrincipal(string sub, string email, string name)
        => new(new ClaimsIdentity(
        [
            new Claim(ClaimTypes.NameIdentifier, sub),
            new Claim(ClaimTypes.Email, email),
            new Claim("name", name),
        ]));
}
