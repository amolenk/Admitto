using Amolenk.Admitto.Organization.Application.Mapping;
using Amolenk.Admitto.Organization.Contracts;
using Amolenk.Admitto.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Organization.Application.UseCases.Users.AssignTeamMembership.AdminApi;

public sealed record AssignTeamMembershipHttpRequest(
    string Email,
    TeamMembershipRoleDto Role)
{
    internal AssignTeamMembershipCommand ToCommand(TeamId teamId)
        => new(
            teamId,
            EmailAddress.From(Email),
            Role.ToDomain());
}