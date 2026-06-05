using Amolenk.Admitto.Core.Organization.Contracts;

namespace Amolenk.Admitto.Core.Organization.Application;

internal class CachingOrganizationFacade(IOrganizationFacade innerFacade)
    : IOrganizationFacade
{
    public ValueTask<Guid?> LookupApiKeyOwnerAsync(
        string keyHash,
        CancellationToken cancellationToken = default) =>
        innerFacade.LookupApiKeyOwnerAsync(keyHash, cancellationToken);
}
