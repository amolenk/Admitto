using Amolenk.Admitto.Core.Organization.Application.UseCases.ApiKeyManagement.ValidateApiKey;
using Amolenk.Admitto.Core.Organization.Contracts;
using Amolenk.Admitto.Core.Shared.Application.Messaging;

namespace Amolenk.Admitto.Core.Organization.Application.UseCases;

internal class OrganizationFacade(IQueryHandler<ValidateApiKeyQuery, Guid?> validateApiKeyHandler) : IOrganizationFacade
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
