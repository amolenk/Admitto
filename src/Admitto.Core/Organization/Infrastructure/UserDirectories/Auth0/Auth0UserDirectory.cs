using Amolenk.Admitto.Core.Organization.Application.ExternalUsers;

namespace Amolenk.Admitto.Core.Organization.Infrastructure.UserDirectories.Auth0;

/// <summary>
/// User directory backed by the Auth0 Management API.
/// Obtains Management API tokens via M2M client credentials with automatic token caching/refresh.
/// </summary>
internal sealed class Auth0UserDirectory(IAuth0ManagementApiClient client) : IExternalUserDirectory
{
    public async ValueTask<string> InviteUserAsync(
        string emailAddress,
        CancellationToken cancellationToken = default)
    {
        // Idempotent: return existing user's ID if already provisioned.
        var existingUserId = await client.FindUserIdByEmailAsync(emailAddress, cancellationToken);
        if (existingUserId is not null)
            return existingUserId;

        // Create new user and send a passkey-enrollment ticket.
        return await client.CreateUserAndSendEnrollmentTicketAsync(emailAddress, cancellationToken);
    }

    public ValueTask DeleteUserAsync(
        string externalUserId,
        CancellationToken cancellationToken = default)
        => client.DeleteUserAsync(externalUserId, cancellationToken);
}
