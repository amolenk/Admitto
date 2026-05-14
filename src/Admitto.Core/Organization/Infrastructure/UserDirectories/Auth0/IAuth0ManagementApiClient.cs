namespace Amolenk.Admitto.Core.Organization.Infrastructure.UserDirectories.Auth0;

/// <summary>
/// Thin abstraction over the Auth0 Management API operations needed by Admitto.
/// Exists to decouple Auth0UserDirectory from the Auth0 SDK types, enabling unit testing.
/// </summary>
internal interface IAuth0ManagementApiClient
{
    /// <summary>Returns the Auth0 user ID for the given email, or null if no user exists.</summary>
    ValueTask<string?> FindUserIdByEmailAsync(string email, CancellationToken cancellationToken = default);

    /// <summary>Creates a new passwordless user and sends a passkey-enrollment ticket. Returns the new user ID.</summary>
    ValueTask<string> CreateUserAndSendEnrollmentTicketAsync(string email, CancellationToken cancellationToken = default);

    /// <summary>Deletes the Auth0 user with the given user ID.</summary>
    ValueTask DeleteUserAsync(string userId, CancellationToken cancellationToken = default);
}
