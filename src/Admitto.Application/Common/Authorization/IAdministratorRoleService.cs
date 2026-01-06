namespace Amolenk.Admitto.Application.Common.Authorization;

public interface IAdministratorRoleService
{
    public bool IsAdministrator(Guid userId);
}