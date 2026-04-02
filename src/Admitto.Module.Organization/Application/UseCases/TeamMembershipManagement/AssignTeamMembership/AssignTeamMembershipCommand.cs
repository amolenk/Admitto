using Amolenk.Admitto.Module.Organization.Contracts;
using Amolenk.Admitto.Module.Shared.Application.Messaging;

namespace Amolenk.Admitto.Module.Organization.Application.UseCases.TeamMembershipManagement.AssignTeamMembership;

internal sealed record AssignTeamMembershipCommand(
    Guid TeamId,
    string EmailAddress,
    TeamMembershipRoleDto Role)
    : Command;