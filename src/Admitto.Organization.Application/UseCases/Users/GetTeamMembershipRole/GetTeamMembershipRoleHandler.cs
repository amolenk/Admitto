using Amolenk.Admitto.Organization.Application.Persistence;
using Amolenk.Admitto.Shared.Application.Messaging;
using Amolenk.Admitto.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Organization.Application.UseCases.GetTeamMembershipRole;

internal class GetTeamMembershipRoleHandler(IOrganizationWriteStore writeStore)
    : IQueryHandler<GetTeamMembershipRoleQuery, TeamMembershipRole?>
{
    public async ValueTask<TeamMembershipRole?> HandleAsync(
        GetTeamMembershipRoleQuery query,
        CancellationToken cancellationToken)
    {
        var role = await writeStore.Users
            .AsNoTracking()
            .Where(u => u.Id == query.UserId)
            .SelectMany(u => u.Memberships)
            .Where(m => m.Id == query.TeamId)
            .Select(m => (TeamMembershipRole?)m.Role)
            .FirstOrDefaultAsync(cancellationToken);

        return role;
    }
}