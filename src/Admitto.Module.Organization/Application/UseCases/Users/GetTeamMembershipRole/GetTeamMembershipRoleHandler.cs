using Amolenk.Admitto.Module.Organization.Application.Mapping;
using Amolenk.Admitto.Module.Organization.Application.Persistence;
using Amolenk.Admitto.Module.Organization.Contracts;
using Amolenk.Admitto.Module.Organization.Domain.ValueObjects;
using Amolenk.Admitto.Module.Shared.Application.Messaging;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Module.Organization.Application.UseCases.Users.GetTeamMembershipRole;

internal sealed class GetTeamMembershipRoleHandler(IOrganizationWriteStore writeStore)
    : IQueryHandler<GetTeamMembershipRoleQuery, TeamMembershipRoleDto?>
{
    public async ValueTask<TeamMembershipRoleDto?> HandleAsync(
        GetTeamMembershipRoleQuery query,
        CancellationToken cancellationToken)
    {
        var externalUserId = ExternalUserId.From(query.UserId);
        var teamId = TeamId.From(query.TeamId);
        
        var role = await writeStore.Users
            .AsNoTracking()
            .Where(u => u.ExternalUserId == externalUserId)
            .SelectMany(u => u.Memberships)
            .Where(m => m.Id == teamId)
            .Select(m => (TeamMembershipRole?)m.Role)
            .FirstOrDefaultAsync(cancellationToken);

        return role?.ToDto();
    }
}