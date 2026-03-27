using System.Text.Json;
using Amolenk.Admitto.Module.Organization.Application.Services;

namespace Amolenk.Admitto.Module.Organization.Infrastructure.UserDirectories.Keycloak;

public class KeycloakUserManagementService(HttpClient client) : IExternalUserDirectory
{
    private const string Realm = "admitto";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = false,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };
    
    public async ValueTask<Guid> UpsertUserAsync(string emailAddress, CancellationToken cancellationToken = default)
    {
        var userId = await GetUserByEmailAsync(emailAddress, cancellationToken);
        if (userId.HasValue) return userId.Value;
        
        return await AddUserAsync(emailAddress, cancellationToken);
    }

    public async ValueTask DeleteUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var response = await client.DeleteAsync($"/admin/realms/{Realm}/users/{userId}", cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new HttpRequestException($"Failed to delete user: {error}", null, response.StatusCode);
        }
    }

    private async ValueTask<Guid?> GetUserByEmailAsync(
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

    private async ValueTask<Guid> AddUserAsync(
        string email,
        CancellationToken cancellationToken = default)
    {
        // Create a user with a fixed password that doesn't require changing
        var newUser = new
        {
            username = email,
            email,
            enabled = true,
            emailVerified = true,
            credentials = new[]
            {
                new
                {
                    type = "password",
                    value = "Password123!", // Fixed password for dev/test
                    temporary = false // Not requiring password change
                }
            }
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
        return Guid.Parse(locationHeader.Split('/').Last());
    }

    private sealed record KeycloakUser(Guid Id);
}