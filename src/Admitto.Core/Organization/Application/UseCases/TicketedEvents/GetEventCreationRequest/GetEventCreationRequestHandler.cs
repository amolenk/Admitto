using Amolenk.Admitto.Core.Organization.Application.Persistence;
using Amolenk.Admitto.Core.Organization.Domain.Entities;
using Amolenk.Admitto.Core.Shared.Application.Messaging;
using Amolenk.Admitto.Core.Shared.Kernel.ErrorHandling;

namespace Amolenk.Admitto.Core.Organization.Application.UseCases.TicketedEvents.GetEventCreationRequest;

internal sealed class GetEventCreationRequestHandler(IOrganizationWriteStore writeStore)
    : IQueryHandler<GetEventCreationRequestQuery, EventCreationRequestDto>
{
    public async ValueTask<EventCreationRequestDto> HandleAsync(
        GetEventCreationRequestQuery query,
        CancellationToken cancellationToken)
    {
        var teamId = TeamId.From(query.TeamId);
        var creationRequestId = CreationRequestId.From(query.CreationRequestId);

        return await writeStore.Teams
                   .AsNoTracking()
                   .Where(t => t.Id == teamId)
                   .SelectMany(t => t.EventCreationRequests)
                   .Where(r => r.Id == creationRequestId)
                   .Select(r => new EventCreationRequestDto(
                       r.Id.Value,
                       query.TeamId,
                       r.RequesterId.Value,
                       r.RequestedAt,
                       r.Status.ToString(),
                       r.CompletedAt,
                       r.TicketedEventId == null ? (Guid?)null : r.TicketedEventId.Value.Value,
                       r.RejectionReason))
                   .FirstOrDefaultAsync(cancellationToken)
               ?? throw new BusinessRuleViolationException(NotFoundError.Create<TeamEventCreationRequest>());
    }
}
