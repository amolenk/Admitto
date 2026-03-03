using Amolenk.Admitto.Organization.Contracts;
using Amolenk.Admitto.Shared.Application.Messaging;

namespace Amolenk.Admitto.Organization.Application.UseCases.Users.GetTeamMembershipRole;

internal sealed record GetTeamMembershipRoleQuery(Guid TeamId, Guid UserId) : Query<TeamMembershipRoleDto?>;