namespace Amolenk.Admitto.Core.Organization.Contracts;

public interface IOrganizationFacade
{
    ValueTask<Guid?> LookupApiKeyOwnerAsync(
        string keyHash,
        CancellationToken cancellationToken = default);
}