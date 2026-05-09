using Amolenk.Admitto.Core.Registrations.Application.Persistence;
using Amolenk.Admitto.Core.Registrations.Domain.ValueObjects;
using Amolenk.Admitto.Core.Shared.Application.Messaging;
using Microsoft.EntityFrameworkCore;

namespace Amolenk.Admitto.Core.Registrations.Application.UseCases.TicketedEventManagement.GetTicketedEvents;

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
                e.Id.Value,
                e.Name.Value,
                e.StartsAt,
                e.EndsAt,
                e.TimeZone.Value,
                e.Status))
            .ToListAsync(cancellationToken);

        return events;
    }
}
