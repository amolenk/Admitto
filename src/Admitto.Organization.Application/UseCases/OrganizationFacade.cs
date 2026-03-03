using Amolenk.Admitto.Organization.Application.UseCases.Teams.GetTeamId;
using Amolenk.Admitto.Organization.Application.UseCases.TicketedEvents.GetTicketedEventId;
using Amolenk.Admitto.Organization.Application.UseCases.TicketedEvents.GetTicketTypes;
using Amolenk.Admitto.Organization.Application.UseCases.Users.GetTeamMembershipRole;
using Amolenk.Admitto.Organization.Contracts;
using Amolenk.Admitto.Shared.Application.Messaging;
using Amolenk.Admitto.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Organization.Application.UseCases;

internal class OrganizationFacade(IMediator mediator) : IOrganizationFacade
{
    public async ValueTask<Guid> GetTeamIdAsync(
        string teamSlug,
        CancellationToken cancellationToken = default)
    {
        var teamId = await mediator.QueryAsync<GetTeamIdQuery, Guid>(
            new GetTeamIdQuery(teamSlug),
            cancellationToken);

        return teamId;
    }

    public async ValueTask<Guid> GetTicketedEventIdAsync(
        Guid teamId,
        string eventSlug,
        CancellationToken cancellationToken = default)
    {
        var ticketedEventId = await mediator.QueryAsync<GetTicketedEventIdQuery, Guid>(
            new GetTicketedEventIdQuery(teamId, eventSlug),
            cancellationToken);

        return ticketedEventId;
    }

    public async ValueTask<TeamMembershipRoleDto?> GetTeamMembershipRoleAsync(
        Guid userId,
        Guid teamId,
        CancellationToken cancellationToken = default)
    {
        var teamMembershipRole = await mediator.QueryAsync<GetTeamMembershipRoleQuery, TeamMembershipRoleDto?>(
            new GetTeamMembershipRoleQuery(teamId, userId),
            cancellationToken);

        return teamMembershipRole;
    }

    public async ValueTask<TicketTypeDto[]> GetTicketTypesAsync(
        Guid eventId,
        CancellationToken cancellationToken = default)
    {
        return await mediator.QueryAsync<GetTicketTypesQuery, TicketTypeDto[]>(
            new GetTicketTypesQuery(eventId),
            cancellationToken);
    }
}