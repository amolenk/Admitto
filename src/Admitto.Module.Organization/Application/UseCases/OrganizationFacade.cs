using Amolenk.Admitto.Module.Organization.Application.UseCases.Teams.GetTeamId;
using Amolenk.Admitto.Module.Organization.Application.UseCases.TicketedEvents.GetTicketedEventId;
using Amolenk.Admitto.Module.Organization.Application.UseCases.TicketedEvents.GetTicketTypes;
using Amolenk.Admitto.Module.Organization.Application.UseCases.Users.GetTeamMembershipRole;
using Amolenk.Admitto.Module.Organization.Contracts;
using Amolenk.Admitto.Module.Shared.Application.Messaging;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Module.Organization.Application.UseCases;

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

    // Event cancellation is not yet supported (planned for FEAT-003).
    // Always returns true (active) until the Organization domain adds event lifecycle management.
    public ValueTask<bool> IsEventActiveAsync(
        Guid eventId,
        CancellationToken cancellationToken = default)
    {
        return ValueTask.FromResult(true);
    }
}