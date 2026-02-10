using System.Text.Json;
using Amolenk.Admitto.Organization.Application.Services;
using Amolenk.Admitto.Organization.Domain.ValueObjects;
using Amolenk.Admitto.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Organization.Infrastructure.UserDirectories.Keycloak;

public class KeycloakUserManagementService(HttpClient client) : IExternalUserDirectory
{
    private const string Realm = "admitto";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = false,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public async ValueTask<ExternalUser?> GetUserByEmailAsync(
        string email,
        CancellationToken cancellationToken = default)
    {
        var response = await client.GetAsync(
            $"/admin/realms/{Realm}/users?email=" + email,
            cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new HttpRequestException($"Failed to retrieve users: {error}", null, response.StatusCode);
        }

        var usersJson = await response.Content.ReadAsStringAsync(cancellationToken);
        var users = JsonSerializer.Deserialize<List<KeycloakUser>>(usersJson, JsonOptions)
                    ?? Enumerable.Empty<KeycloakUser>();

        return users.Select(u => u.ToUser()).FirstOrDefault();
    }

    public async ValueTask<IEnumerable<ExternalUser>> GetUsersAsync(CancellationToken cancellationToken)
    {
        var response = await client.GetAsync($"/admin/realms/{Realm}/users", cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new HttpRequestException($"Failed to retrieve users: {error}", null, response.StatusCode);
        }

        var usersJson = await response.Content.ReadAsStringAsync(cancellationToken);
        var users = JsonSerializer.Deserialize<List<KeycloakUser>>(usersJson, JsonOptions)
                    ?? Enumerable.Empty<KeycloakUser>();

        return users.Select(u => u.ToUser());
    }

    public async ValueTask<ExternalUser> AddUserAsync(
        string email,
        string firstName,
        string lastName,
        CancellationToken cancellationToken = default)
    {
        // Create a user with a fixed password that doesn't require changing
        var newUser = new
        {
            username = email,
            email,
            firstName,
            lastName,
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
        var userId = new UserId(Guid.Parse(locationHeader.Split('/').Last()));

        // Return a User domain object
        return new ExternalUser(userId, EmailAddress.From(email));
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

    private record KeycloakUser(string Id, string Email)
    {
        public ExternalUser ToUser() => new(new UserId(Guid.Parse(Id)), EmailAddress.From(Email));
    }
}