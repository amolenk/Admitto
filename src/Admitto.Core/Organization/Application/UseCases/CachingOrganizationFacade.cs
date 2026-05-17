using Amolenk.Admitto.Core.Organization.Contracts;

namespace Amolenk.Admitto.Core.Organization.Application.UseCases;

internal class CachingOrganizationFacade(IOrganizationFacade innerFacade)
    : IOrganizationFacade
{
    public ValueTask<Guid?> ValidateApiKeyAsync(
        string keyHash,
        CancellationToken cancellationToken = default) =>
        innerFacade.ValidateApiKeyAsync(keyHash, cancellationToken);
}