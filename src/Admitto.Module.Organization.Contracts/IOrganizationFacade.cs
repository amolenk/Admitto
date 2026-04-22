namespace Amolenk.Admitto.Module.Organization.Contracts;

public interface IOrganizationFacade
{
    ValueTask<Guid> GetTeamIdAsync(
        string teamSlug,
        CancellationToken cancellationToken = default);

    ValueTask<TeamMembershipRoleDto?> GetTeamMembershipRoleAsync(
        Guid userId,
        Guid teamId,
        CancellationToken cancellationToken = default);
}