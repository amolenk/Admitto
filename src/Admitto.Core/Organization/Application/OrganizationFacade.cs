using Amolenk.Admitto.Core.Organization.Application.UseCases.ApiKeys.GetApiKeyOwner;
using Amolenk.Admitto.Core.Organization.Application.Persistence;
using Amolenk.Admitto.Core.Organization.Contracts;
using Amolenk.Admitto.Core.Shared.Application.Messaging;

namespace Amolenk.Admitto.Core.Organization.Application;

internal class OrganizationFacade(
    IQueryHandler<GetApiKeyOwnerQuery, Guid?> getApiKeyOwnerHandler,
    IOrganizationWriteStore writeStore) : IOrganizationFacade
{
    public async ValueTask<Guid?> GetApiKeyOwnerAsync(
        string keyHash,
        CancellationToken cancellationToken = default)
    {
        return await getApiKeyOwnerHandler.HandleAsync(
            new GetApiKeyOwnerQuery(keyHash),
            cancellationToken);
    }

    public async ValueTask<TeamBrandingDto?> GetTeamBrandingAsync(
        Guid teamId,
        CancellationToken cancellationToken = default)
    {
        var typedTeamId = TeamId.From(teamId);
        return await writeStore.Teams
            .AsNoTracking()
            .Where(t => t.Id == typedTeamId)
            .Select(t => new TeamBrandingDto(t.Id.Value, t.AccentColor.Value))
            .FirstOrDefaultAsync(cancellationToken);
    }
}
