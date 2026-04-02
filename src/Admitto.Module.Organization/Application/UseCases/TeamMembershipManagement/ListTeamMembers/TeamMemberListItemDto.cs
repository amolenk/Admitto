using Amolenk.Admitto.Module.Organization.Contracts;

namespace Amolenk.Admitto.Module.Organization.Application.UseCases.TeamMembershipManagement.ListTeamMembers;

public sealed record TeamMemberListItemDto(string Email, TeamMembershipRoleDto Role);
