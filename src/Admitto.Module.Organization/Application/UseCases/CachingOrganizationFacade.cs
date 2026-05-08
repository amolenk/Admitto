using Amolenk.Admitto.Module.Organization.Contracts;

namespace Amolenk.Admitto.Module.Organization.Application.UseCases;

internal class CachingOrganizationFacade(IOrganizationFacade innerFacade)
    : IOrganizationFacade
{
    public ValueTask<TeamMembershipRoleDto?> GetTeamMembershipRoleAsync(
        Guid userId,
        Guid teamId,
        CancellationToken cancellationToken = default) =>
        innerFacade.GetTeamMembershipRoleAsync(userId, teamId, cancellationToken);

    public ValueTask<Guid?> ValidateApiKeyAsync(
        string keyHash,
        CancellationToken cancellationToken = default) =>
        innerFacade.ValidateApiKeyAsync(keyHash, cancellationToken);
}