using Amolenk.Admitto.Module.Organization.Application.Persistence;
using Amolenk.Admitto.Module.Shared.Application.Messaging;
using Microsoft.EntityFrameworkCore;

namespace Amolenk.Admitto.Module.Organization.Application.UseCases.ApiKeyManagement.ValidateApiKey;

internal sealed class ValidateApiKeyHandler(IOrganizationWriteStore writeStore)
    : IQueryHandler<ValidateApiKeyQuery, Guid?>
{
    public async ValueTask<Guid?> HandleAsync(ValidateApiKeyQuery query, CancellationToken cancellationToken)
    {
        var result = await writeStore.ApiKeys
            .AsNoTracking()
            .Where(k => k.KeyHash == query.KeyHash && k.RevokedAt == null)
            .Select(k => new { k.TeamId })
            .FirstOrDefaultAsync(cancellationToken);

        return result?.TeamId.Value;
    }
}
