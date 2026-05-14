using System.Text.Json;
using Amolenk.Admitto.Core.Organization.Application.Services;

namespace Amolenk.Admitto.Core.Organization.Infrastructure.UserDirectories.Keycloak;

public class KeycloakUserManagementService(HttpClient client) : IExternalUserDirectory
{
    private const string Realm = "admitto";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = false,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };
    
    public async ValueTask<string> InviteUserAsync(string emailAddress, CancellationToken cancellationToken = default)
    {
        var userId = await GetUserByEmailAsync(emailAddress, cancellationToken);
        if (userId is not null) return userId;
        
        return await AddUserAsync(emailAddress, cancellationToken);
    }

    public async ValueTask DeleteUserAsync(string externalUserId, CancellationToken cancellationToken = default)
    {
        var response = await client.DeleteAsync($"/admin/realms/{Realm}/users/{externalUserId}", cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new HttpRequestException($"Failed to delete user: {error}", null, response.StatusCode);
        }
    }

    private async ValueTask<string?> GetUserByEmailAsync(
        string emailAddress,
        CancellationToken cancellationToken = default)
    {
        var response = await client.GetAsync(
            $"/admin/realms/{Realm}/users?email=" + emailAddress,
            cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new HttpRequestException($"Failed to retrieve users: {error}", null, response.StatusCode);
        }

        var usersJson = await response.Content.ReadAsStringAsync(cancellationToken);
        var users = JsonSerializer.Deserialize<List<KeycloakUser>>(usersJson, JsonOptions)
                    ?? Enumerable.Empty<KeycloakUser>();

        return users.Select(u => u.Id).FirstOrDefault();
    }

    private async ValueTask<string> AddUserAsync(
        string email,
        CancellationToken cancellationToken = default)
    {
        var newUser = new
        {
            username = email,
            email,
            enabled = true,
            requiredActions = new[] { "webauthn-register-passwordless" }
        };

        var content = new StringContent(
            JsonSerializer.Serialize(newUser, JsonOptions),
            System.Text.Encoding.UTF8,
            "application/json");

        var response = await client.PostAsync($"/admin/realms/{Realm}/users", content, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new HttpRequestException($"Failed to create user: {error}", null, response.StatusCode);
        }

        // Extract user ID from the Location header
        var locationHeader = response.Headers.Location?.ToString();
        if (string.IsNullOrEmpty(locationHeader))
        {
            throw new InvalidOperationException($"User was created but the Location header is missing");
        }

        // The Location header format is "/admin/realms/{realm}/users/{userId}"
        var userId = locationHeader.Split('/').Last();

        // Send the execute-actions email so the user can register their passkey
        await SendExecuteActionsEmailAsync(userId, cancellationToken);

        return userId;
    }

    private async ValueTask SendExecuteActionsEmailAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        var actions = new[] { "webauthn-register-passwordless" };

        var content = new StringContent(
            JsonSerializer.Serialize(actions, JsonOptions),
            System.Text.Encoding.UTF8,
            "application/json");

        var response = await client.PutAsync(
            $"/admin/realms/{Realm}/users/{userId}/execute-actions-email",
            content,
            cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new HttpRequestException($"Failed to send execute-actions email: {error}", null, response.StatusCode);
        }
    }

    private sealed record KeycloakUser(string Id);
}