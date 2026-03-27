namespace Amolenk.Admitto.Module.Organization.Application.Services;

public interface IExternalUserDirectory
{
    ValueTask<Guid> UpsertUserAsync(string emailAddress, CancellationToken cancellationToken = default);

    ValueTask DeleteUserAsync(Guid userId, CancellationToken cancellationToken = default);
}