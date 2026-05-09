using Amolenk.Admitto.Core.Shared.Application.Messaging;

namespace Amolenk.Admitto.Core.Organization.Application.UseCases.TeamMembershipManagement.ListTeamMembers;

internal sealed record GetTeamMembersQuery(Guid TeamId) : Query<IReadOnlyList<TeamMemberListItemDto>>;
