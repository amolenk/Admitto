using Amolenk.Admitto.Application.Common.Abstractions;
using Microsoft.Graph;
using Microsoft.Graph.Models;
using User = Amolenk.Admitto.Domain.ValueObjects.User;

namespace Amolenk.Admitto.Infrastructure.UserManagement.MicrosoftGraph;

public class MicrosoftGraphIdentityService(GraphServiceClient graphServiceClient) : IIdentityService
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
        // For Entra ID, we invite guest users instead of creating actual users
        var invitation = new Invitation
        {
            InvitedUserEmailAddress = email,
            InviteRedirectUrl = "https://www.sandermolenkamp.com",
            SendInvitationMessage = true,
            InvitedUserDisplayName = email.Split('@')[0]
        };

        var createdInvitation = await graphServiceClient.Invitations.PostAsync(invitation, cancellationToken: cancellationToken);
        
        if (createdInvitation?.InvitedUser?.Id == null)
        {
            throw new InvalidOperationException("Guest invitation was sent but user ID is missing");
        }

        return new User(Guid.Parse(createdInvitation.InvitedUser.Id), email);
    }

    public async ValueTask DeleteUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        await graphServiceClient.Users[userId.ToString()].DeleteAsync(cancellationToken: cancellationToken);
    }
}