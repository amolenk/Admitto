using Amolenk.Admitto.Core.Organization.Contracts;

namespace Amolenk.Admitto.Core.Organization.Application.UseCases.TeamMemberships.ListTeamMembers;

public sealed record TeamMemberListItemDto(string Email, TeamMembershipRoleDto Role);
