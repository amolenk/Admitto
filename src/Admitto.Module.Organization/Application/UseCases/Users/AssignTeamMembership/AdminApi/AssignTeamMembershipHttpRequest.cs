using Amolenk.Admitto.Module.Organization.Contracts;

namespace Amolenk.Admitto.Module.Organization.Application.UseCases.Users.AssignTeamMembership.AdminApi;

public sealed record AssignTeamMembershipHttpRequest(
    string Email,
    TeamMembershipRoleDto Role)
{
    internal AssignTeamMembershipCommand ToCommand(Guid teamId)
        => new(teamId, Email, Role);
}