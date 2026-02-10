using Amolenk.Admitto.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Organization.Application.Services;

public interface IExternalUserDirectory
{
    ValueTask<ExternalUser?> GetUserByEmailAsync(EmailAddress email, CancellationToken cancellationToken = default);

    ValueTask<IEnumerable<ExternalUser>> GetUsersAsync(CancellationToken cancellationToken = default);

    ValueTask<ExternalUser> AddUserAsync(
        EmailAddress email,
        // string firstName,
        // string lastName,
        CancellationToken cancellationToken = default);

    ValueTask DeleteUserAsync(Guid userId, CancellationToken cancellationToken = default);
}