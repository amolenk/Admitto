using Amolenk.Admitto.Domain.ValueObjects;
using Microsoft.Extensions.Caching.Memory;

namespace Amolenk.Admitto.Application.Common.Authorization;

public class CachingTeamMemberRoleService(
    ITeamMemberRoleService innerService,
    IMemoryCache cache,
    ILogger<CachingTeamMemberRoleService> logger) : ITeamMemberRoleService
{
    public async ValueTask<IEnumerable<Guid>> GetTeamsAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var key = $"teams:{userId}";
        
        return await cache.GetOrCreateAsync(
            key,
            async entry =>
            {
                logger.LogDebug("Cache miss for user teams: {CacheKey}", key);

                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5);

                return (await innerService.GetTeamsAsync(userId, cancellationToken)).ToArray();
            }) ?? [];
    }

    public async ValueTask<TeamMemberRole?> GetTeamMemberRoleAsync(
        Guid userId,
        Guid teamId,
        CancellationToken cancellationToken = default)
    {
        var key = $"member:{userId}:{teamId}";
        
        return await cache.GetOrCreateAsync(
            key,
            async entry =>
            {
                logger.LogDebug("Cache miss for user team member role: {CacheKey}", key);

                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5);

                return await innerService.GetTeamMemberRoleAsync(userId, teamId, cancellationToken);
            });
    }
}