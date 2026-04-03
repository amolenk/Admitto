using Amolenk.Admitto.Module.Organization.Application.Persistence;
using Amolenk.Admitto.Module.Organization.Domain.Entities;
using Amolenk.Admitto.Module.Organization.Domain.ValueObjects;
using Amolenk.Admitto.Module.Shared.Application.Messaging;
using Amolenk.Admitto.Module.Shared.Kernel.ErrorHandling;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Module.Organization.Application.UseCases.TicketedEvents.GetTicketedEvent;

internal sealed class GetTicketedEventHandler(IOrganizationWriteStore writeStore)
    : IQueryHandler<GetTicketedEventQuery, TicketedEventDto>
{
    public async ValueTask<TicketedEventDto> HandleAsync(
        GetTicketedEventQuery query,
        CancellationToken cancellationToken)
    {
        var teamId = TeamId.From(query.TeamId);
        var eventSlug = Slug.From(query.EventSlug);

        var dto = await writeStore.TicketedEvents
            .AsNoTracking()
            .Where(e => e.TeamId == teamId && e.Slug == eventSlug)
            .Select(e => new TicketedEventDto(
                e.Slug.Value,
                e.Name.Value,
                e.WebsiteUrl.Value.ToString(),
                e.BaseUrl.Value.ToString(),
                e.EventWindow.Start,
                e.EventWindow.End,
                e.Status.ToString(),
                e.Version,
                e.TicketTypes.Select(tt => new TicketTypeDto(
                    tt.Slug.Value,
                    tt.Name.Value,
                    tt.IsSelfService,
                    tt.IsSelfServiceAvailable,
                    tt.TimeSlots.Select(ts => ts.Slug.Value).ToList(),
                    tt.Capacity == null ? (int?)null : tt.Capacity.Value.Value,
                    tt.IsCancelled)).ToList()))
            .FirstOrDefaultAsync(cancellationToken);

        return dto ?? throw new BusinessRuleViolationException(
            NotFoundError.Create<TicketedEvent>(query.EventSlug));
    }
}
