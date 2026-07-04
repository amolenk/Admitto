namespace Amolenk.Admitto.Core.Organization.Contracts;

public interface IOrganizationFacade
{
    ValueTask<Guid?> GetApiKeyOwnerAsync(
        string keyHash,
        CancellationToken cancellationToken = default);
}
