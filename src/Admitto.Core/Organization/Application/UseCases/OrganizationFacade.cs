using Amolenk.Admitto.Core.Organization.Application.UseCases.ApiKeyManagement.ValidateApiKey;
using Amolenk.Admitto.Core.Organization.Application.UseCases.Users.GetTeamMembershipRole;
using Amolenk.Admitto.Core.Organization.Contracts;

namespace Amolenk.Admitto.Core.Organization.Application.UseCases;

internal class OrganizationFacade(
    GetTeamMembershipRoleHandler getTeamMembershipRoleHandler,
    ValidateApiKeyHandler validateApiKeyHandler) : IOrganizationFacade
{
    public async ValueTask<TeamMembershipRoleDto?> GetTeamMembershipRoleAsync(
        Guid userId,
        Guid teamId,
        CancellationToken cancellationToken = default)
    {
        return await getTeamMembershipRoleHandler.HandleAsync(
            new GetTeamMembershipRoleQuery(teamId, userId),
            cancellationToken);
    }

    public async ValueTask<Guid?> ValidateApiKeyAsync(
        string keyHash,
        CancellationToken cancellationToken = default)
    {
        return await validateApiKeyHandler.HandleAsync(
            new ValidateApiKeyQuery(keyHash),
            cancellationToken);
    }
}