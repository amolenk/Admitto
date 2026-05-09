using Amolenk.Admitto.Core.Module.Organization.Contracts;
using Amolenk.Admitto.Core.Shared.Application.Messaging;

namespace Amolenk.Admitto.Core.Module.Organization.Application.UseCases.TeamMembershipManagement.ChangeTeamMembershipRole;

internal sealed record ChangeTeamMembershipRoleCommand(
    Guid TeamId,
    string EmailAddress,
    TeamMembershipRoleDto NewRole)
    : Command;
