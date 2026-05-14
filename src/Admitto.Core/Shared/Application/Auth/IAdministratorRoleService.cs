namespace Amolenk.Admitto.Core.Shared.Application.Auth;

/// <summary>
/// Service for checking whether a user holds the administrator role.
/// </summary>
public interface IAdministratorRoleService
{
    /// <summary>Returns <c>true</c> if the given user is an administrator.</summary>
    ValueTask<bool> IsAdministratorAsync(Guid userId, CancellationToken cancellationToken = default);
}
