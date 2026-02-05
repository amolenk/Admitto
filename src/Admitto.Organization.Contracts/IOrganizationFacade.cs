using Amolenk.Admitto.Shared.Contracts;

namespace Amolenk.Admitto.Organization.Contracts;

public interface IOrganizationFacade
{
    ValueTask<Guid> ResolveTeamIdAsync(
        string teamSlug,
        CancellationToken cancellationToken = default);

    ValueTask<Guid> ResolveEventIdAsync(
        Guid teamId,
        string eventSlug,
        CancellationToken cancellationToken = default);

    ValueTask<TeamMemberRoleDto> GetTeamMemberRoleAsync(
        Guid userId,
        Guid teamId,
        CancellationToken cancellationToken = default);

    ValueTask<TicketTypeDto[]> GetTicketTypesAsync(
        Guid eventId,
        CancellationToken cancellationToken = default);
}