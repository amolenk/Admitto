using Amolenk.Admitto.Core.Organization.Infrastructure.UserDirectories.Auth0;
using NSubstitute;
using Shouldly;

namespace Amolenk.Admitto.Core.DomainTests.Organization.Infrastructure.UserDirectories.Auth0;

[TestClass]
public sealed class Auth0UserDirectoryTests
{
    private IAuth0ManagementApiClient _apiClient = null!;
    private Auth0UserDirectory _sut = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        _apiClient = Substitute.For<IAuth0ManagementApiClient>();
        _sut = new Auth0UserDirectory(_apiClient);
    }

    [TestMethod]
    public async Task InviteNewUser_CreatesUserAndSendsEnrollmentTicket()
    {
        // Arrange
        // SC-INVITE-NEW: When a user does not yet exist in Auth0, InviteUserAsync creates a
        // new account, sends a passkey-enrollment ticket, and returns the new user ID.
        const string email = "newuser@example.com";
        const string newUserId = "auth0|new123";

        _apiClient.FindUserIdByEmailAsync(email, Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult<string?>(null));

        _apiClient.CreateUserAndSendEnrollmentTicketAsync(email, Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(newUserId));

        // Act
        var result = await _sut.InviteUserAsync(email);

        // Assert
        result.ShouldBe(newUserId);

        await _apiClient.Received(1)
            .FindUserIdByEmailAsync(email, Arg.Any<CancellationToken>());

        await _apiClient.Received(1)
            .CreateUserAndSendEnrollmentTicketAsync(email, Arg.Any<CancellationToken>());
    }

    [TestMethod]
    public async Task InviteExistingUser_ReturnsExistingIdWithoutCreating()
    {
        // Arrange
        // SC-INVITE-IDEMPOTENT: When a user already exists in Auth0 (e.g., from a previous
        // failed commit), InviteUserAsync returns the existing ID without creating a duplicate.
        const string email = "existing@example.com";
        const string existingUserId = "auth0|existing456";

        _apiClient.FindUserIdByEmailAsync(email, Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult<string?>(existingUserId));

        // Act
        var result = await _sut.InviteUserAsync(email);

        // Assert
        result.ShouldBe(existingUserId);

        await _apiClient.Received(1)
            .FindUserIdByEmailAsync(email, Arg.Any<CancellationToken>());

        await _apiClient.DidNotReceive()
            .CreateUserAndSendEnrollmentTicketAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [TestMethod]
    public async Task DeleteUser_DelegatesToApiClient()
    {
        // Arrange
        // SC-DEPROVISION: When DeleteUserAsync is called, it delegates to the Management API
        // to remove the user account.
        const string userId = "auth0|todelete789";

        _apiClient.DeleteUserAsync(userId, Arg.Any<CancellationToken>())
            .Returns(ValueTask.CompletedTask);

        // Act
        await _sut.DeleteUserAsync(userId);

        // Assert
        await _apiClient.Received(1)
            .DeleteUserAsync(userId, Arg.Any<CancellationToken>());
    }
}
