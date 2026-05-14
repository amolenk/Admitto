using Auth0.ManagementApi;
using Microsoft.Extensions.Options;

namespace Amolenk.Admitto.Core.Organization.Infrastructure.UserDirectories.Auth0;

/// <summary>
/// Wraps the Auth0 Management API SDK and implements <see cref="IAuth0ManagementApiClient"/>.
/// Creates a fresh <see cref="ManagementClient"/> per call so token caching and disposal
/// are handled by the SDK itself.
/// </summary>
internal sealed class Auth0ManagementApiAdapter(IOptions<Auth0Options> options) : IAuth0ManagementApiClient
{
    private readonly Auth0Options _options = options.Value;

    // Connection name for the passwordless user database in Auth0.
    private const string Connection = "Username-Password-Authentication";

    public async ValueTask<string?> FindUserIdByEmailAsync(
        string email,
        CancellationToken cancellationToken = default)
    {
        using var client = CreateManagementClient();

        var users = await client.Users.ListUsersByEmailAsync(
            new ListUsersByEmailRequestParameters { Email = email },
            cancellationToken: cancellationToken);

        return users.FirstOrDefault()?.UserId;
    }

    public async ValueTask<string> CreateUserAndSendEnrollmentTicketAsync(
        string email,
        CancellationToken cancellationToken = default)
    {
        using var client = CreateManagementClient();

        var created = await client.Users.CreateAsync(
            new CreateUserRequestContent
            {
                Connection = Connection,
                Email = email,
                EmailVerified = false,
                VerifyEmail = false
            },
            cancellationToken: cancellationToken);

        // The change-password ticket redirects to the Admin UI after passkey enrollment.
        await client.Tickets.ChangePasswordAsync(
            new ChangePasswordTicketRequestContent
            {
                UserId = created.UserId,
                MarkEmailAsVerified = true
            },
            cancellationToken: cancellationToken);

        return created.UserId!;
    }

    public async ValueTask DeleteUserAsync(string userId, CancellationToken cancellationToken = default)
    {
        using var client = CreateManagementClient();
        await client.Users.DeleteAsync(userId, cancellationToken: cancellationToken);
    }

    private ManagementClient CreateManagementClient() =>
        new(new ManagementClientOptions
        {
            Domain = _options.Domain,
            TokenProvider = new ClientCredentialsTokenProvider(
                domain: _options.Domain,
                clientId: _options.ClientId,
                clientSecret: _options.ClientSecret)
        });
}
