using Amolenk.Admitto.Core.Organization.Application.Mapping;
using Amolenk.Admitto.Core.Organization.Application.Persistence;
using Amolenk.Admitto.Core.Organization.Contracts;
using Amolenk.Admitto.Core.Organization.Domain.ValueObjects;
using Amolenk.Admitto.Core.Shared.Application.Messaging;
using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Core.Organization.Application.UseCases.Users.GetTeamMembershipRole;

internal sealed class GetTeamMembershipRoleHandler(IOrganizationWriteStore writeStore)
    : IQueryHandler<GetTeamMembershipRoleQuery, TeamMembershipRoleDto?>
{
    public async ValueTask<TeamMembershipRoleDto?> HandleAsync(
        GetTeamMembershipRoleQuery query,
        CancellationToken cancellationToken)
    {
        var userId = UserId.From(query.UserId);
        var teamId = TeamId.From(query.TeamId);
        
        var role = await writeStore.Users
            .AsNoTracking()
            .Where(u => u.Id == userId)
            .SelectMany(u => u.Memberships)
            .Where(m => m.Id == teamId)
            .Select(m => (TeamMembershipRole?)m.Role)
            .FirstOrDefaultAsync(cancellationToken);

        return role?.ToDto();
    }
}