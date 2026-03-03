using Amolenk.Admitto.Organization.Contracts;
using Microsoft.Extensions.Caching.Memory;

namespace Amolenk.Admitto.Organization.Application.UseCases;

internal class CachingOrganizationFacade(IOrganizationFacade innerFacade, IMemoryCache memoryCache)
    : IOrganizationFacade
{
    private static readonly TimeSpan TeamIdCacheDuration = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan EventIdCacheDuration = TimeSpan.FromMinutes(5);

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

    public async ValueTask<Guid> GetTicketedEventIdAsync(
        Guid teamId,
        string eventSlug,
        CancellationToken cancellationToken = default)
    {
        var cacheKey = $"org:eventId:{teamId}:{eventSlug}";
        if (memoryCache.TryGetValue<Guid>(cacheKey, out var eventId))
        {
            return eventId;
        }

        var result = await innerFacade.GetTicketedEventIdAsync(teamId, eventSlug, cancellationToken);

        memoryCache.Set(cacheKey, result, DateTimeOffset.Now.Add(EventIdCacheDuration));

        return result;
    }

    public ValueTask<TeamMembershipRoleDto?> GetTeamMembershipRoleAsync(
        Guid userId,
        Guid teamId,
        CancellationToken cancellationToken = default) =>
        innerFacade.GetTeamMembershipRoleAsync(userId, teamId, cancellationToken);

    public ValueTask<TicketTypeDto[]> GetTicketTypesAsync(
        Guid eventId,
        CancellationToken cancellationToken = default) =>
        innerFacade.GetTicketTypesAsync(eventId, cancellationToken);
}