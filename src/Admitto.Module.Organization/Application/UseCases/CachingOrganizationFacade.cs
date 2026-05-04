using Amolenk.Admitto.Module.Organization.Contracts;
using Microsoft.Extensions.Caching.Memory;

namespace Amolenk.Admitto.Module.Organization.Application.UseCases;

internal class CachingOrganizationFacade(IOrganizationFacade innerFacade, IMemoryCache memoryCache)
    : IOrganizationFacade
{
    private static readonly TimeSpan TeamIdCacheDuration = TimeSpan.FromMinutes(5);

    public async ValueTask<Guid> GetTeamIdAsync(
        string teamSlug,
        CancellationToken cancellationToken = default)
    {
        var cacheKey = $"org:teamId:{teamSlug}";
        if (memoryCache.TryGetValue<Guid>(cacheKey, out var teamId))
        {
            return teamId;
        }

        var result = await innerFacade.GetTeamIdAsync(teamSlug, cancellationToken);

        memoryCache.Set(cacheKey, result, DateTimeOffset.Now.Add(TeamIdCacheDuration));

        return result;
    }

    public ValueTask<TeamMembershipRoleDto?> GetTeamMembershipRoleAsync(
        Guid userId,
        Guid teamId,
        CancellationToken cancellationToken = default) =>
        innerFacade.GetTeamMembershipRoleAsync(userId, teamId, cancellationToken);

    public ValueTask<Guid?> ValidateApiKeyAsync(
        string keyHash,
        CancellationToken cancellationToken = default) =>
        innerFacade.ValidateApiKeyAsync(keyHash, cancellationToken);
}