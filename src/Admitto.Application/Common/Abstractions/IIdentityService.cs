using Amolenk.Admitto.Domain.ValueObjects;

namespace Amolenk.Admitto.Application.Common.Abstractions;

// TODO Rename to UserManagementService 
public interface IIdentityService
{
    ValueTask<User?> GetUserByEmailAsync(string email, CancellationToken cancellationToken = default);

    ValueTask<IEnumerable<User>> GetUsersAsync(CancellationToken cancellationToken = default);

    ValueTask<User> AddUserAsync(string email, CancellationToken cancellationToken = default);

    ValueTask DeleteUserAsync(Guid userId, CancellationToken cancellationToken = default);
}