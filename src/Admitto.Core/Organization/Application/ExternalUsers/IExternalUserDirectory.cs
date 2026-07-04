namespace Amolenk.Admitto.Core.Organization.Application.ExternalUsers;

public interface IExternalUserDirectory
{
    ValueTask<string> InviteUserAsync(string emailAddress, CancellationToken cancellationToken = default);

    ValueTask DeleteUserAsync(string externalUserId, CancellationToken cancellationToken = default);
}
