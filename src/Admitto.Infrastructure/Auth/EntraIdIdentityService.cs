using Amolenk.Admitto.Application.Common.Abstractions;
using Amolenk.Admitto.Domain.ValueObjects;
using Microsoft.Graph;
using Microsoft.Graph.Models;

namespace Amolenk.Admitto.Infrastructure.Auth;

public class EntraIdIdentityService(GraphServiceClient graphServiceClient) : IIdentityService
{
    public async ValueTask<User?> GetUserByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        try
        {
            var users = await graphServiceClient.Users
                .GetAsync(requestConfiguration =>
                {
                    requestConfiguration.QueryParameters.Filter = $"mail eq '{email}' or userPrincipalName eq '{email}'";
                    requestConfiguration.QueryParameters.Select = new[] { "id", "mail", "userPrincipalName" };
                }, cancellationToken);

            var user = users?.Value?.FirstOrDefault();
            if (user?.Id == null) return null;
            
            // Use mail if available, otherwise fall back to userPrincipalName
            var userEmail = user.Mail ?? user.UserPrincipalName;
            return !string.IsNullOrEmpty(userEmail) ? new User(Guid.Parse(user.Id), userEmail) : null;
        }
        catch (ServiceException ex) when (ex.ResponseStatusCode == 404)
        {
            return null;
        }
    }

    public async ValueTask<IEnumerable<User>> GetUsersAsync(CancellationToken cancellationToken = default)
    {
        var users = await graphServiceClient.Users
            .GetAsync(requestConfiguration =>
            {
                requestConfiguration.QueryParameters.Select = new[] { "id", "mail", "userPrincipalName" };
                requestConfiguration.QueryParameters.Filter = "accountEnabled eq true";
            }, cancellationToken);

        return users?.Value?.Where(u => !string.IsNullOrEmpty(u.Mail) || !string.IsNullOrEmpty(u.UserPrincipalName))
            .Select(u => new User(Guid.Parse(u.Id!), u.Mail ?? u.UserPrincipalName!)) ?? Enumerable.Empty<User>();
    }

    public async ValueTask<User> AddUserAsync(string email, CancellationToken cancellationToken = default)
    {
        // Extract display name from email for better user experience
        var displayName = email.Split('@')[0];
        var userPrincipalName = email;

        var newUser = new Microsoft.Graph.Models.User
        {
            DisplayName = displayName,
            Mail = email,
            UserPrincipalName = userPrincipalName,
            AccountEnabled = true,
            // For Entra ID, we don't set a password - user will be invited or password will be set via other means
            PasswordProfile = new PasswordProfile
            {
                ForceChangePasswordNextSignIn = true,
                Password = Guid.NewGuid().ToString("N")[..8] + "Temp!"
            }
        };

        var createdUser = await graphServiceClient.Users.PostAsync(newUser, cancellationToken);
        
        if (createdUser?.Id == null)
        {
            throw new InvalidOperationException("User was created but the ID is missing");
        }

        return new User(Guid.Parse(createdUser.Id), email);
    }

    public async ValueTask DeleteUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        await graphServiceClient.Users[userId.ToString()].DeleteAsync(cancellationToken);
    }
}