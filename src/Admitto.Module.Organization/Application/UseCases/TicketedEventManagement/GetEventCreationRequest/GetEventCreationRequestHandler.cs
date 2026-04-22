using Amolenk.Admitto.Module.Organization.Application.Persistence;
using Amolenk.Admitto.Module.Organization.Domain.Entities;
using Amolenk.Admitto.Module.Organization.Domain.ValueObjects;
using Amolenk.Admitto.Module.Shared.Application.Messaging;
using Amolenk.Admitto.Module.Shared.Kernel.ErrorHandling;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Module.Organization.Application.UseCases.TicketedEventManagement.GetEventCreationRequest;

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
                       r.RequestedSlug.Value,
                       r.RequesterId.Value,
                       r.RequestedAt,
                       r.Status.ToString(),
                       r.CompletedAt,
                       r.TicketedEventId == null ? (Guid?)null : r.TicketedEventId.Value.Value,
                       r.RejectionReason))
                   .FirstOrDefaultAsync(cancellationToken)
               ?? throw new BusinessRuleViolationException(
                   NotFoundError.Create<TeamEventCreationRequest>(query.CreationRequestId));
    }
}
