namespace Amolenk.Admitto.Module.Shared.Application.Auth;

/// <summary>
/// Service for checking whether a user holds the administrator role.
/// </summary>
public interface IAdministratorRoleService
{
    /// <summary>Returns <c>true</c> if the given user is an administrator.</summary>
    bool IsAdministrator(Guid userId);
}
