using Amolenk.Admitto.Core.Organization.Application.UseCases.ApiKeys.ValidateApiKey;
using Amolenk.Admitto.Core.Organization.Contracts;
using Amolenk.Admitto.Core.Shared.Application.Messaging;

namespace Amolenk.Admitto.Core.Organization.Application;

internal class OrganizationFacade(IQueryHandler<ValidateApiKeyQuery, Guid?> validateApiKeyHandler) : IOrganizationFacade
{
    public async ValueTask<Guid?> LookupApiKeyOwnerAsync(
        string keyHash,
        CancellationToken cancellationToken = default)
    {
        return await validateApiKeyHandler.HandleAsync(
            new ValidateApiKeyQuery(keyHash),
            cancellationToken);
    }
}
