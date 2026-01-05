using Amolenk.Admitto.Domain.ValueObjects;

namespace Amolenk.Admitto.Application.Common.Authentication;

public interface IUserManagementService
{
    ValueTask<User?> GetUserByEmailAsync(string email, CancellationToken cancellationToken = default);

    ValueTask<IEnumerable<User>> GetUsersAsync(CancellationToken cancellationToken = default);

    ValueTask<User> AddUserAsync(
        string email,
        string firstName,
        string lastName,
        CancellationToken cancellationToken = default);

    ValueTask DeleteUserAsync(Guid userId, CancellationToken cancellationToken = default);
}