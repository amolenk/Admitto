using Amolenk.Admitto.Organization.Application.UseCases.GetTicketTypes;
using Amolenk.Admitto.Organization.Application.UseCases.ResolveEventId;
using Amolenk.Admitto.Organization.Application.UseCases.ResolveTeamId;
using Amolenk.Admitto.Organization.Contracts;
using Amolenk.Admitto.Organization.Domain.ValueObjects;
using Amolenk.Admitto.Shared.Application.Messaging;
using Amolenk.Admitto.Shared.Contracts;
using Amolenk.Admitto.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Organization.Application;

internal class OrganizationFacade(IMediator mediator) : IOrganizationFacade
{
    public async ValueTask<Guid> ResolveTeamIdAsync(
        string teamSlug,
        CancellationToken cancellationToken = default)
    {
        return await mediator.QueryAsync<ResolveTeamIdQuery, Guid>(
            new ResolveTeamIdQuery(TeamSlug.From(teamSlug)),
            cancellationToken);
    }
    
    public async ValueTask<Guid> ResolveEventIdAsync(
        Guid teamId,
        string eventSlug,
        CancellationToken cancellationToken = default)
    {
        return await mediator.QueryAsync<ResolveEventIdQuery, Guid>(
            new ResolveEventIdQuery(new TeamId(teamId), TicketedEventSlug.From(eventSlug)),
            cancellationToken);
    }

    public ValueTask<TeamMemberRoleDto> GetTeamMemberRoleAsync(
        Guid userId,
        Guid teamId,
        CancellationToken cancellationToken = default)
    {
        // TODO Implement
        // For now, simple regard everyone as Organizer
        return ValueTask.FromResult(TeamMemberRoleDto.Organizer);
    }

    public async ValueTask<TicketTypeDto[]> GetTicketTypesAsync(
        Guid eventId,
        CancellationToken cancellationToken = default)
    {
        return await mediator.QueryAsync<GetTicketTypesQuery, TicketTypeDto[]>(
            new GetTicketTypesQuery(new TicketedEventId(eventId)),
            cancellationToken);
    }
}