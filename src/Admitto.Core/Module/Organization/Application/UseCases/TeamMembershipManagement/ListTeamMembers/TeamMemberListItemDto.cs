using Amolenk.Admitto.Core.Module.Organization.Contracts;

namespace Amolenk.Admitto.Core.Module.Organization.Application.UseCases.TeamMembershipManagement.ListTeamMembers;

public sealed record TeamMemberListItemDto(string Email, TeamMembershipRoleDto Role);
