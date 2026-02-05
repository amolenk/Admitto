namespace Amolenk.Admitto.ApiService.Auth;

public interface IAdministratorRoleService
{
    bool IsAdministrator(Guid userId);
}

public class AdministratorRoleService(IConfiguration configuration) : IAdministratorRoleService
{
    private readonly Guid[] _adminUserIds = configuration.GetSection("Authentication:AdminUserIds").Get<Guid[]>()
                                            ?? [];

    public bool IsAdministrator(Guid userId) => _adminUserIds.Contains(userId);
}