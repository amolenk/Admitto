using Amolenk.Admitto.Core.Module.Organization.Contracts;

namespace Amolenk.Admitto.Core.Module.Organization.Application.UseCases.TeamMembershipManagement.AssignTeamMembership.AdminApi;

public sealed record AssignTeamMembershipHttpRequest(
    string Email,
    TeamMembershipRoleDto Role)
{
    internal AssignTeamMembershipCommand ToCommand(Guid teamId)
        => new(teamId, Email, Role);
}