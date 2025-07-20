using Amolenk.Admitto.Application.Common.Validation;
using Amolenk.Admitto.Domain.Entities;

namespace Amolenk.Admitto.Application.Common.Data;

public static class TicketEventQueryExtensions
{
    public static async ValueTask<(Guid TeamId, Guid TicketedEventId)> GetTicketedEventIdsAsync(
        this IDomainContext context,
        string teamSlug,
        string eventSlug,
        CancellationToken cancellationToken = default)
    {
        var ids = await context.Teams
            .AsNoTracking()
            .Join(
                context.TicketedEvents,
                t => t.Id,
                e => e.TeamId,
                (t, e) => new { Team = t, Event = e })
            .Where(x => x.Team.Slug == teamSlug && x.Event.Slug == eventSlug)
            .Select(x => new
            {
                TeamId = x.Team.Id,
                EventId = x.Event.Id
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (ids is null)
        {
            // TODO
            throw new Exception("Ticketed event not found for the provided team and event slug.");
        }

        return (ids.TeamId, ids.EventId);
    }

    public static async ValueTask<TicketedEvent> GetTicketedEventAsync(
        this IQueryable<TicketedEvent> ticketedEvents,
        Guid teamId,
        string eventSlug,
        bool noTracking = false,
        CancellationToken cancellationToken = default)
    {
        if (noTracking)
        {
            ticketedEvents = ticketedEvents.AsNoTracking();
        }

        var ticketedEvent = await ticketedEvents
            .FirstOrDefaultAsync(t => t.TeamId == teamId && t.Slug == eventSlug, cancellationToken);

        if (ticketedEvent is null)
        {
            throw ValidationError.TicketedEvent.NotFound(teamId);
        }

        return ticketedEvent;
    }
}