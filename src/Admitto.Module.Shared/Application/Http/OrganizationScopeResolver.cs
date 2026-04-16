namespace Amolenk.Admitto.Module.Shared.Application.Http;

public interface IOrganizationScopeResolver
{
    ValueTask<OrganizationScope> ResolveAsync(
        string teamSlug,
        string? eventSlug = null,
        CancellationToken cancellationToken = default);
}