using Amolenk.Admitto.Module.Organization.Contracts;

namespace Amolenk.Admitto.Module.Organization.Application.UseCases.TeamMembershipManagement.ChangeTeamMembershipRole.AdminApi;

public sealed record ChangeTeamMembershipRoleHttpRequest(TeamMembershipRoleDto NewRole)
{
    internal ChangeTeamMembershipRoleCommand ToCommand(Guid teamId, string email)
        => new(teamId, email, NewRole);
}
