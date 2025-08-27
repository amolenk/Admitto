using Amolenk.Admitto.Application.Common.Abstractions;
using Amolenk.Admitto.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace Amolenk.Admitto.Infrastructure.Persistence;

public class SlugResolver(IApplicationContext applicationContext, IMemoryCache memoryCache) : ISlugResolver
{
    public async ValueTask<Guid> GetTeamIdAsync(string teamSlug, CancellationToken cancellationToken = default)
    {
        if (memoryCache.TryGetValue(teamSlug, out Guid teamId))
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

        memoryCache.Set(teamSlug, teamId);
        return teamId;
    }
    
    public async ValueTask<Guid> GetTicketedEventIdAsync(
        Guid teamId,
        string eventSlug,
        CancellationToken cancellationToken = default)
    {
        var cacheKey = $"{teamId}:{eventSlug}";

        if (memoryCache.TryGetValue(cacheKey, out Guid eventId))
        {
            return eventId;
        }

        var ticketedEventId = await applicationContext.TicketedEvents
            .AsNoTracking()
            .Where(e => e.TeamId == teamId && e.Slug == eventSlug)
            .Select(e => e.Id)
            .FirstOrDefaultAsync(cancellationToken);
            
        if (ticketedEventId == Guid.Empty)
        {
            throw new DomainRuleException(DomainRuleError.TicketedEvent.NotFound(eventSlug));
        }

        memoryCache.Set(cacheKey, ticketedEventId);
        return ticketedEventId;
    }

    public async ValueTask<(Guid TeamId, Guid TicketedEventId)> GetTeamAndTicketedEventsIdsAsync(
        string teamSlug,
        string eventSlug,
        CancellationToken cancellationToken = default)
    {
        var teamId = await GetTeamIdAsync(teamSlug, cancellationToken);
        var eventId = await GetTicketedEventIdAsync(teamId, eventSlug, cancellationToken);
        
        return (teamId, eventId);
    }
}