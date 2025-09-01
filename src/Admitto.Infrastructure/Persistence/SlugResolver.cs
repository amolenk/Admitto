using Amolenk.Admitto.Application.Common.Abstractions;
using Amolenk.Admitto.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace Amolenk.Admitto.Infrastructure.Persistence;

// TODO Can be done smarter

public class SlugResolver(IApplicationContext applicationContext, IMemoryCache memoryCache) : ISlugResolver
{
    public async ValueTask<Guid> ResolveTeamIdAsync(string teamSlug, CancellationToken cancellationToken = default)
    {
        var cacheKey = $"slug:{teamSlug}";
        
        if (memoryCache.TryGetValue(cacheKey, out Guid teamId))
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

        memoryCache.Set(cacheKey, teamId);
        return teamId;
    }
    
    public async ValueTask<Guid> ResolveTicketedEventIdAsync(string teamSlug, string eventSlug, CancellationToken cancellationToken = default)
    {
        var (_, eventId) = await ResolveTeamAndTicketedEventIdsAsync(teamSlug, eventSlug, cancellationToken);

        return eventId;
    }

    public async ValueTask<(Guid TeamId, Guid TicketedEventId)> ResolveTeamAndTicketedEventIdsAsync(
        string teamSlug,
        string eventSlug,
        CancellationToken cancellationToken = default)
    {
        var teamId = await ResolveTeamIdAsync(teamSlug, cancellationToken);
        var eventId = await GetTicketedEventIdAsync(teamId, eventSlug, cancellationToken);
        
        return (teamId, eventId);
    }
    
    private async ValueTask<Guid> GetTicketedEventIdAsync(
        Guid teamId,
        string eventSlug,
        CancellationToken cancellationToken = default)
    {
        var cacheKey = $"slug:{teamId}:{eventSlug}";
        
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
}