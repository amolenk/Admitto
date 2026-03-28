using Amolenk.Admitto.Module.Shared.Contracts;

namespace Amolenk.Admitto.Module.Organization.Contracts;

public interface IOrganizationFacade
{
    ValueTask<Guid> GetTeamIdAsync(
        string teamSlug,
        CancellationToken cancellationToken = default);

    ValueTask<Guid> GetTicketedEventIdAsync(
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

    ValueTask<bool> IsEventActiveAsync(
        Guid eventId,
        CancellationToken cancellationToken = default);
}