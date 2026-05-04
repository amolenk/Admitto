using Amolenk.Admitto.Module.Organization.Application.UseCases.ApiKeyManagement.ValidateApiKey;
using Amolenk.Admitto.Module.Organization.Application.UseCases.TeamManagement.GetTeamId;
using Amolenk.Admitto.Module.Organization.Application.UseCases.Users.GetTeamMembershipRole;
using Amolenk.Admitto.Module.Organization.Contracts;
using Amolenk.Admitto.Module.Shared.Application.Messaging;

namespace Amolenk.Admitto.Module.Organization.Application.UseCases;

internal class OrganizationFacade(IMediator mediator) : IOrganizationFacade
{
    public async ValueTask<Guid> GetTeamIdAsync(
        string teamSlug,
        CancellationToken cancellationToken = default)
    {
        var teamId = await mediator.QueryAsync<GetTeamIdQuery, Guid>(
            new GetTeamIdQuery(teamSlug),
            cancellationToken);

        return teamId;
    }

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