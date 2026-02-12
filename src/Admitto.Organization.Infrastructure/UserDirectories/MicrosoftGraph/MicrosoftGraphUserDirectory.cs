using Amolenk.Admitto.Organization.Application.Services;
using Microsoft.Graph;
using Microsoft.Graph.Models;

namespace Amolenk.Admitto.Organization.Infrastructure.UserDirectories.MicrosoftGraph;

public class MicrosoftGraphUserManagementService(GraphServiceClient graphServiceClient) : IExternalUserDirectory
{
    public async ValueTask<Guid> UpsertUserAsync(string emailAddress, CancellationToken cancellationToken = default)
    {
        var userId = await GetUserByEmailAsync(emailAddress, cancellationToken);
        if (userId.HasValue) return userId.Value;
        
        return await AddUserAsync(emailAddress, cancellationToken);
    }

    public async ValueTask DeleteUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        await graphServiceClient.Users[userId.ToString()].DeleteAsync(cancellationToken: cancellationToken);
    }
    
    private async ValueTask<Guid?> GetUserByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        try
        {
            var users = await graphServiceClient.Users
                .GetAsync(
                    requestConfiguration =>
                    {
                        requestConfiguration.QueryParameters.Filter =
                            $"mail eq '{email}' or userPrincipalName eq '{email}'";
                        requestConfiguration.QueryParameters.Select = ["id", "mail", "userPrincipalName"];
                    },
                    cancellationToken);

            var user = users?.Value?.FirstOrDefault();
            
            if (user?.Id == null) return null;

            // Use mail if available, otherwise fall back to userPrincipalName
            var userEmail = user.Mail ?? user.UserPrincipalName;
            return !string.IsNullOrEmpty(userEmail) ? Guid.Parse(user.Id) : null;
        }
        catch (ServiceException ex) when (ex.ResponseStatusCode == 404)
        {
            return null;
        }
    }

    private async ValueTask<Guid> AddUserAsync(string emailAddress, CancellationToken cancellationToken = default)
    {
        // For Entra ID, we invite guest users instead of creating actual users
        var invitation = new Invitation
        {
            InvitedUserEmailAddress = emailAddress,
            InviteRedirectUrl = "https://www.admitto.org", // TODO Make configurable
        };

        var createdInvitation = await graphServiceClient.Invitations.PostAsync(
            invitation,
            cancellationToken: cancellationToken);

        return createdInvitation?.InvitedUser?.Id == null
            ? throw new InvalidOperationException("Guest invitation was sent but user ID is missing")
            : Guid.Parse(createdInvitation.InvitedUser.Id);
    }
}