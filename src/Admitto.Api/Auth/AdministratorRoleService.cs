using Amolenk.Admitto.Core.Organization.Application.Persistence;
using Amolenk.Admitto.Core.Organization.Domain.ValueObjects;
using Amolenk.Admitto.Core.Shared.Application.Auth;
using Microsoft.EntityFrameworkCore;

namespace Amolenk.Admitto.ApiService.Auth;

/// <summary>
/// DB-backed administrator role service that checks the <c>IsAdmin</c> flag on the domain <see cref="User"/> entity.
/// </summary>
public class AdministratorRoleService(IOrganizationWriteStore store) : IAdministratorRoleService
{
    public async ValueTask<bool> IsAdministratorAsync(Guid userId, CancellationToken cancellationToken = default) =>
        await store.Users
            .Where(u => u.Id == UserId.From(userId) && u.IsAdmin)
            .AnyAsync(cancellationToken);
}