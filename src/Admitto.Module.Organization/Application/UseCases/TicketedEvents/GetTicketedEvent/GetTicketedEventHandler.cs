using Amolenk.Admitto.Module.Organization.Application.Persistence;
using Amolenk.Admitto.Module.Organization.Domain.Entities;
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

        var entity = await writeStore.TicketedEvents
            .AsNoTracking()
            .Where(e => e.TeamId == teamId && e.Slug == eventSlug)
            .FirstOrDefaultAsync(cancellationToken);

        if (entity is null)
            throw new BusinessRuleViolationException(
                NotFoundError.Create<TicketedEvent>(query.EventSlug));

        return new TicketedEventDto(
            entity.Slug.Value,
            entity.Name.Value,
            entity.WebsiteUrl.Value.ToString(),
            entity.BaseUrl.Value.ToString(),
            entity.EventWindow.Start,
            entity.EventWindow.End,
            entity.Status.ToString(),
            entity.Version);
    }
}
