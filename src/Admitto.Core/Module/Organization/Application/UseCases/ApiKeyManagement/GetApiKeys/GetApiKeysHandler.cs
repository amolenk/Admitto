using Amolenk.Admitto.Core.Module.Organization.Application.Persistence;
using Amolenk.Admitto.Core.Module.Organization.Domain.ValueObjects;
using Amolenk.Admitto.Core.Shared.Application.Messaging;
using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Core.Module.Organization.Application.UseCases.ApiKeyManagement.GetApiKeys;

internal sealed class GetApiKeysHandler(IOrganizationWriteStore writeStore)
    : IQueryHandler<GetApiKeysQuery, IReadOnlyList<ApiKeyListItemDto>>
{
    public async ValueTask<IReadOnlyList<ApiKeyListItemDto>> HandleAsync(
        GetApiKeysQuery query,
        CancellationToken cancellationToken)
    {
        var teamId = TeamId.From(query.TeamId);

        return await writeStore.ApiKeys
            .AsNoTracking()
            .Where(k => k.TeamId == teamId)
            .OrderByDescending(k => k.CreatedAt)
            .Select(k => new ApiKeyListItemDto(
                k.Id.Value,
                k.Name,
                k.KeyPrefix,
                k.CreatedAt,
                k.CreatedBy,
                k.RevokedAt))
            .ToListAsync(cancellationToken);
    }
}
