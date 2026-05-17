using Amolenk.Admitto.Core.Organization.Application.UseCases.ApiKeyManagement.ValidateApiKey;
using Amolenk.Admitto.Core.Organization.Contracts;

namespace Amolenk.Admitto.Core.Organization.Application.UseCases;

internal class OrganizationFacade(ValidateApiKeyHandler validateApiKeyHandler) : IOrganizationFacade
{
    public async ValueTask<Guid?> ValidateApiKeyAsync(
        string keyHash,
        CancellationToken cancellationToken = default)
    {
        return await validateApiKeyHandler.HandleAsync(
            new ValidateApiKeyQuery(keyHash),
            cancellationToken);
    }
}