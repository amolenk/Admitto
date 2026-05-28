using Amolenk.Admitto.Core.Organization.Contracts;

namespace Amolenk.Admitto.Core.Organization.Application.UseCases.TeamMemberships.AssignTeamMembership.AdminApi;

public sealed record AssignTeamMembershipHttpRequest(
    string Email,
    TeamMembershipRoleDto Role)
{
    internal AssignTeamMembershipCommand ToCommand(Guid teamId)
        => new(teamId, Email, Role);
}