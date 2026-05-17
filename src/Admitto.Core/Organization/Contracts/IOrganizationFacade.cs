namespace Amolenk.Admitto.Core.Organization.Contracts;

public interface IOrganizationFacade
{
    ValueTask<Guid?> ValidateApiKeyAsync(
        string keyHash,
        CancellationToken cancellationToken = default);
}