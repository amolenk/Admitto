using Amolenk.Admitto.Core.Organization.Contracts;
using Amolenk.Admitto.Core.Shared.Application.Messaging;

namespace Amolenk.Admitto.Core.Organization.Application.UseCases.TeamMemberships.ChangeTeamMembershipRole;

internal sealed record ChangeTeamMembershipRoleCommand(
    Guid TeamId,
    string EmailAddress,
    TeamMembershipRoleDto NewRole)
    : Command;
