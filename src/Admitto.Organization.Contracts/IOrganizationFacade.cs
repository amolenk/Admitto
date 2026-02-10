using Amolenk.Admitto.Shared.Contracts;

namespace Amolenk.Admitto.Organization.Contracts;

public interface IOrganizationFacade
{
    ValueTask<Guid> GetTeamIdAsync(
        string teamSlug,
        CancellationToken cancellationToken = default);

    ValueTask<Guid> GetEventIdAsync(
        Guid teamId,
        string eventSlug,
        CancellationToken cancellationToken = default);

    ValueTask<TeamMembershipRoleDto?> GetTeamMembershipRoleAsync(
        Guid userId,
        Guid teamId,
        CancellationToken cancellationToken = default);

    ValueTask<TicketTypeDto[]> GetTicketTypesAsync(
        Guid eventId,
        CancellationToken cancellationToken = default);
}