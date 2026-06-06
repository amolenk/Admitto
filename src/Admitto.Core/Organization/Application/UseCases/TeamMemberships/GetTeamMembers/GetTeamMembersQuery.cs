using Amolenk.Admitto.Core.Shared.Application.Messaging;

namespace Amolenk.Admitto.Core.Organization.Application.UseCases.TeamMemberships.GetTeamMembers;

internal sealed record GetTeamMembersQuery(Guid TeamId) : Query<IReadOnlyList<TeamMemberListItemDto>>;
