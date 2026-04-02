using Amolenk.Admitto.Module.Shared.Application.Auth;

namespace Amolenk.Admitto.ApiService.Auth;

/// <summary>
/// Reads administrator user IDs from configuration and implements <see cref="IAdministratorRoleService"/>.
/// </summary>
public class AdministratorRoleService(IConfiguration configuration) : IAdministratorRoleService
{
    private readonly Guid[] _adminUserIds = configuration.GetSection("Authentication:AdminUserIds").Get<Guid[]>()
                                            ?? [];

    public bool IsAdministrator(Guid userId) => _adminUserIds.Contains(userId);
}