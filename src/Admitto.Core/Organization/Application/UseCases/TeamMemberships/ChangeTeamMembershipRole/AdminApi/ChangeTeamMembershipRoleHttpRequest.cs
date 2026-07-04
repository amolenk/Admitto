using Amolenk.Admitto.Core.Organization.Contracts;

namespace Amolenk.Admitto.Core.Organization.Application.UseCases.TeamMemberships.ChangeTeamMembershipRole.AdminApi;

public sealed record ChangeTeamMembershipRoleHttpRequest(TeamMembershipRoleDto NewRole)
{
    internal ChangeTeamMembershipRoleCommand ToCommand(Guid teamId, string email)
        => new(teamId, email, NewRole);
}
