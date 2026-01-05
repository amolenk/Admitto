namespace Amolenk.Admitto.Application.Common.Authorization;

public class AdministratorRoleService(IConfiguration configuration) : IAdministratorRoleService
{
    private readonly Guid[] _adminUserIds = configuration.GetSection("Authentication:AdminUserIds").Get<Guid[]>()
                                            ?? [];

    public bool IsAdministrator(Guid userId) => _adminUserIds.Contains(userId);
}