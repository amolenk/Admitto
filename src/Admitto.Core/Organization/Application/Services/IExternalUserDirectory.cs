namespace Amolenk.Admitto.Core.Organization.Application.Services;

public interface IExternalUserDirectory
{
    ValueTask<string> InviteUserAsync(string emailAddress, CancellationToken cancellationToken = default);

    ValueTask DeleteUserAsync(string externalUserId, CancellationToken cancellationToken = default);
}