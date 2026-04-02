using Amolenk.Admitto.Module.Shared.Application.Messaging;

namespace Amolenk.Admitto.Module.Organization.Application.UseCases.TeamMembershipManagement.ListTeamMembers;

internal sealed record GetTeamMembersQuery(Guid TeamId) : Query<IReadOnlyList<TeamMemberListItemDto>>;
