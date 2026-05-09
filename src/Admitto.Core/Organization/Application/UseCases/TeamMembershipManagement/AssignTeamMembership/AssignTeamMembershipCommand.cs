using Amolenk.Admitto.Core.Organization.Contracts;
using Amolenk.Admitto.Core.Shared.Application.Messaging;

namespace Amolenk.Admitto.Core.Organization.Application.UseCases.TeamMembershipManagement.AssignTeamMembership;

internal sealed record AssignTeamMembershipCommand(
    Guid TeamId,
    string EmailAddress,
    TeamMembershipRoleDto Role)
    : Command;