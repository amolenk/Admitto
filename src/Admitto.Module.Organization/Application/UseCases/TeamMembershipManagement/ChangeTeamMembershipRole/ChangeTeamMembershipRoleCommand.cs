using Amolenk.Admitto.Module.Organization.Contracts;
using Amolenk.Admitto.Module.Shared.Application.Messaging;

namespace Amolenk.Admitto.Module.Organization.Application.UseCases.TeamMembershipManagement.ChangeTeamMembershipRole;

internal sealed record ChangeTeamMembershipRoleCommand(
    Guid TeamId,
    string EmailAddress,
    TeamMembershipRoleDto NewRole)
    : Command;
