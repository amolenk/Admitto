namespace Amolenk.Admitto.Module.Shared.Application.Http;

public interface IOrganizationScopeResolver
{
    ValueTask<OrganizationScope> ResolveAsync(CancellationToken cancellationToken = default);
}