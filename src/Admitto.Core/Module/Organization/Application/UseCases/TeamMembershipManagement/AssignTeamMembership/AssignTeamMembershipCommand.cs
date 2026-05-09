using Amolenk.Admitto.Core.Module.Organization.Contracts;
using Amolenk.Admitto.Core.Shared.Application.Messaging;

namespace Amolenk.Admitto.Core.Module.Organization.Application.UseCases.TeamMembershipManagement.AssignTeamMembership;

internal sealed record AssignTeamMembershipCommand(
    Guid TeamId,
    string EmailAddress,
    TeamMembershipRoleDto Role)
    : Command;