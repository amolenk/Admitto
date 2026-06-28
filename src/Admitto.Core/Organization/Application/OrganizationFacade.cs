using Amolenk.Admitto.Core.Organization.Application.UseCases.ApiKeys.GetApiKeyOwner;
using Amolenk.Admitto.Core.Organization.Application.Persistence;
using Amolenk.Admitto.Core.Organization.Contracts;
using Amolenk.Admitto.Core.Shared.Application.Messaging;

namespace Amolenk.Admitto.Core.Organization.Application;

internal class OrganizationFacade(
    IQueryHandler<GetApiKeyOwnerQuery, Guid?> getApiKeyOwnerHandler) : IOrganizationFacade
{
    public async ValueTask<Guid?> GetApiKeyOwnerAsync(
        string keyHash,
        CancellationToken cancellationToken = default)
    {
        return await getApiKeyOwnerHandler.HandleAsync(
            new GetApiKeyOwnerQuery(keyHash),
            cancellationToken);
    }

}
