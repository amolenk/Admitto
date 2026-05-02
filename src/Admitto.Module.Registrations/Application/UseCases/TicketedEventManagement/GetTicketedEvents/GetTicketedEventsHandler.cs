using Amolenk.Admitto.Module.Registrations.Application.Persistence;
using Amolenk.Admitto.Module.Registrations.Domain.ValueObjects;
using Amolenk.Admitto.Module.Shared.Application.Messaging;
using Microsoft.EntityFrameworkCore;

namespace Amolenk.Admitto.Module.Registrations.Application.UseCases.TicketedEventManagement.GetTicketedEvents;

internal sealed class GetTicketedEventsHandler(IRegistrationsWriteStore writeStore)
    : IQueryHandler<GetTicketedEventsQuery, IReadOnlyList<TicketedEventListItemDto>>
{
    public async ValueTask<IReadOnlyList<TicketedEventListItemDto>> HandleAsync(
        GetTicketedEventsQuery query,
        CancellationToken cancellationToken)
    {
        var teamId = query.TeamId;

        var events = await writeStore.TicketedEvents
            .AsNoTracking()
            .Where(e => e.TeamId == teamId && e.Status != EventLifecycleStatus.Archived)
            .OrderByDescending(e => e.StartsAt)
            .Select(e => new TicketedEventListItemDto(
                e.Slug.Value,
                e.Name.Value,
                e.StartsAt,
                e.EndsAt,
                e.TimeZone.Value,
                e.Status))
            .ToListAsync(cancellationToken);

        return events;
    }
}
