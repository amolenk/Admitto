using Amolenk.Admitto.Organization.Contracts;
using Amolenk.Admitto.Shared.Application.Messaging;

namespace Amolenk.Admitto.Organization.Application.UseCases.Users.AssignTeamMembership;

internal sealed record AssignTeamMembershipCommand(
    Guid TeamId,
    string EmailAddress,
    TeamMembershipRoleDto Role)
    : Command;