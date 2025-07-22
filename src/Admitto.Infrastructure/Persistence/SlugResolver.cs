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

        var team = await applicationContext.Teams
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Slug == teamSlug, cancellationToken);

        if (team is null)
        {
            throw new BusinessRuleException(BusinessRuleError.Team.NotFound(teamSlug));
        }

        memoryCache.CreateEntry(teamSlug).Value = team.Id;
        return team.Id;
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

        var ticketedEvent = await applicationContext.TicketedEvents
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.TeamId == teamId && t.Slug == eventSlug, cancellationToken);

        if (ticketedEvent is null)
        {
            throw new BusinessRuleException(BusinessRuleError.TicketedEvent.NotFound(eventSlug));
        }

        memoryCache.CreateEntry(cacheKey).Value = ticketedEvent.Id;
        return ticketedEvent.Id;
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