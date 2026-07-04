using Amolenk.Admitto.Core.Organization.Application.Persistence;
using Amolenk.Admitto.Core.Shared.Application.Messaging;

namespace Amolenk.Admitto.Core.Organization.Application.UseCases.ApiKeys.GetApiKeyOwner;

internal sealed class GetApiKeyOwnerHandler(IOrganizationWriteStore writeStore)
    : IQueryHandler<GetApiKeyOwnerQuery, Guid?>
{
    public async ValueTask<Guid?> HandleAsync(GetApiKeyOwnerQuery query, CancellationToken cancellationToken)
    {
        var result = await writeStore.ApiKeys
            .AsNoTracking()
            .Where(k => k.KeyHash == query.KeyHash && k.RevokedAt == null)
            .Select(k => new { k.TeamId })
            .FirstOrDefaultAsync(cancellationToken);

        return result?.TeamId.Value;
    }
}
