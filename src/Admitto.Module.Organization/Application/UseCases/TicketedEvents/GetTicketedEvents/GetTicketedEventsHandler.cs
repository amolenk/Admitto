using Amolenk.Admitto.Module.Organization.Application.Persistence;
using Amolenk.Admitto.Module.Organization.Domain.ValueObjects;
using Amolenk.Admitto.Module.Shared.Application.Messaging;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Module.Organization.Application.UseCases.TicketedEvents.GetTicketedEvents;

internal sealed class GetTicketedEventsHandler(IOrganizationWriteStore writeStore)
    : IQueryHandler<GetTicketedEventsQuery, TicketedEventListItemDto[]>
{
    public async ValueTask<TicketedEventListItemDto[]> HandleAsync(
        GetTicketedEventsQuery query,
        CancellationToken cancellationToken)
    {
        var teamId = TeamId.From(query.TeamId);

        return await writeStore.TicketedEvents
            .AsNoTracking()
            .Where(e => e.TeamId == teamId && e.Status != EventStatus.Archived)
            .OrderBy(e => e.EventWindow.Start)
            .Select(e => new TicketedEventListItemDto(
                e.Slug.Value,
                e.Name.Value,
                e.EventWindow.Start,
                e.EventWindow.End,
                e.Status.ToString()))
            .ToArrayAsync(cancellationToken);
    }
}
