using Amolenk.Admitto.Application.Common.Persistence;
using Amolenk.Admitto.Domain;
using Microsoft.Extensions.Caching.Memory;

namespace Amolenk.Admitto.Application.Common.Slugs;

public interface ISlugResolver
{
    ValueTask<Guid> ResolveTeamIdAsync(string teamSlug, CancellationToken cancellationToken = default);
    
    ValueTask<Guid> ResolveTicketedEventIdAsync(
        string teamSlug,
        string eventSlug,
        CancellationToken cancellationToken = default);

    ValueTask<(Guid TeamId, Guid TicketedEventId)> ResolveTeamAndTicketedEventIdsAsync(
        string teamSlug,
        string eventSlug,
        CancellationToken cancellationToken = default);
}

// TODO Add cache TTL
public class SlugResolver(IApplicationContext applicationContext, IMemoryCache cache)
    : ISlugResolver
{
    public async ValueTask<Guid> ResolveTeamIdAsync(string teamSlug, CancellationToken cancellationToken = default)
    {
        if (TryGetCachedTeamId(teamSlug, out var teamId))
        {
            return teamId;
        }

        teamId = await applicationContext.Teams
            .AsNoTracking()
            .Where(t => t.Slug == teamSlug)
            .Select(t => t.Id)
            .FirstOrDefaultAsync(cancellationToken);

        if (teamId == Guid.Empty)
        {
            throw new DomainRuleException(DomainRuleError.Team.NotFound(teamSlug));
        }

        CacheTeamId(teamSlug, teamId);
        return teamId;
    }
    
    public async ValueTask<Guid> ResolveTicketedEventIdAsync(
        string teamSlug,
        string eventSlug,
        CancellationToken cancellationToken = default)
    {
        if (TryGetCachedTicketedEventId(teamSlug, eventSlug, out var cachedEventId))
        {
            return cachedEventId;
        }
        
        var (_, eventId) = await ResolveTeamAndTicketedEventIdsAsync(teamSlug, eventSlug, cancellationToken);

        return eventId;
    }

    public async ValueTask<(Guid TeamId, Guid TicketedEventId)> ResolveTeamAndTicketedEventIdsAsync(
        string teamSlug,
        string eventSlug,
        CancellationToken cancellationToken = default)
    {
        var teamId = await ResolveTeamIdAsync(teamSlug, cancellationToken);

        if (TryGetCachedTicketedEventId(teamSlug, eventSlug, out var eventId))
        {
            return (teamId, eventId);
        }

        eventId = await applicationContext.TicketedEvents
            .AsNoTracking()
            .Where(t => t.TeamId == teamId && t.Slug == eventSlug)
            .Select(t => t.Id)
            .FirstOrDefaultAsync(cancellationToken);

        if (eventId == Guid.Empty)
        {
            throw new DomainRuleException(DomainRuleError.TicketedEvent.NotFound(eventSlug));
        }

        CacheTicketedEventId(teamSlug, eventSlug, eventId);

        return (teamId, eventId);
    }

    private bool TryGetCachedTeamId(string teamSlug, out Guid teamId)
        => cache.TryGetValue(GetCacheKey(teamSlug), out teamId);

    private bool TryGetCachedTicketedEventId(string teamSlug, string eventSlug, out Guid eventId)
        => cache.TryGetValue(GetCacheKey(teamSlug, eventSlug), out eventId);

    private void CacheTeamId(string teamSlug, Guid teamId)
        => cache.Set(GetCacheKey(teamSlug), teamId);

    private void CacheTicketedEventId(string teamSlug, string eventSlug, Guid eventId)
        => cache.Set(GetCacheKey(teamSlug, eventSlug), eventId);

    private static string GetCacheKey(string teamSlug) => $"team:{teamSlug}";

    private static string GetCacheKey(string teamSlug, string eventSlug) => $"{teamSlug}:{eventSlug}";
}