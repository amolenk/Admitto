using Amolenk.Admitto.Organization.Application.Mapping;
using Amolenk.Admitto.Organization.Application.UseCases.GetEventId;
using Amolenk.Admitto.Organization.Application.UseCases.GetTicketTypes;
using Amolenk.Admitto.Organization.Application.UseCases.Teams.GetTeamId;
using Amolenk.Admitto.Organization.Application.UseCases.Users.GetTeamMembershipRole;
using Amolenk.Admitto.Organization.Contracts;
using Amolenk.Admitto.Organization.Domain.ValueObjects;
using Amolenk.Admitto.Shared.Application.Messaging;
using Amolenk.Admitto.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Organization.Application;

internal class OrganizationFacade(IMediator mediator) : IOrganizationFacade
{
    public async ValueTask<Guid> GetTeamIdAsync(
        string teamSlug,
        CancellationToken cancellationToken = default)
    {
        var teamId = await mediator.QueryAsync<GetTeamIdQuery, TeamId>(
            new GetTeamIdQuery(Slug.From(teamSlug)),
            cancellationToken);
        
        return teamId.Value;
    }
    
    public async ValueTask<Guid> GetEventIdAsync(
        Guid teamId,
        string eventSlug,
        CancellationToken cancellationToken = default)
    {
        var ticketedEventId = await mediator.QueryAsync<GetEventIdQuery, TicketedEventId>(
            new GetEventIdQuery(TeamId.From(teamId), Slug.From(eventSlug)),
            cancellationToken);

        return ticketedEventId.Value;
    }

    public async ValueTask<TeamMembershipRoleDto?> GetTeamMembershipRoleAsync(
        Guid userId,
        Guid teamId,
        CancellationToken cancellationToken = default)
    {
        var teamMembershipRole = await mediator.QueryAsync<GetTeamMembershipRoleQuery, TeamMembershipRole?>(
            new GetTeamMembershipRoleQuery(TeamId.From(teamId), UserId.From(userId)),
            cancellationToken);

        return teamMembershipRole?.ToDto();
    }

    public async ValueTask<TicketTypeDto[]> GetTicketTypesAsync(
        Guid eventId,
        CancellationToken cancellationToken = default)
    {
        return await mediator.QueryAsync<GetTicketTypesQuery, TicketTypeDto[]>(
            new GetTicketTypesQuery(TicketedEventId.From(eventId)),
            cancellationToken);
    }
}