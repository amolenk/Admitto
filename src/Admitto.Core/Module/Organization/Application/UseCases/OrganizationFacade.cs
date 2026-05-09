using Amolenk.Admitto.Core.Module.Organization.Application.UseCases.ApiKeyManagement.ValidateApiKey;
using Amolenk.Admitto.Core.Module.Organization.Application.UseCases.Users.GetTeamMembershipRole;
using Amolenk.Admitto.Core.Module.Organization.Contracts;
using Amolenk.Admitto.Core.Shared.Application.Messaging;

namespace Amolenk.Admitto.Core.Module.Organization.Application.UseCases;

internal class OrganizationFacade(IMediator mediator) : IOrganizationFacade
{
    public async ValueTask<TeamMembershipRoleDto?> GetTeamMembershipRoleAsync(
        Guid userId,
        Guid teamId,
        CancellationToken cancellationToken = default)
    {
        var teamMembershipRole = await mediator.QueryAsync<GetTeamMembershipRoleQuery, TeamMembershipRoleDto?>(
            new GetTeamMembershipRoleQuery(teamId, userId),
            cancellationToken);

        return teamMembershipRole;
    }

    public async ValueTask<Guid?> ValidateApiKeyAsync(
        string keyHash,
        CancellationToken cancellationToken = default)
    {
        return await mediator.QueryAsync<ValidateApiKeyQuery, Guid?>(
            new ValidateApiKeyQuery(keyHash),
            cancellationToken);
    }
}