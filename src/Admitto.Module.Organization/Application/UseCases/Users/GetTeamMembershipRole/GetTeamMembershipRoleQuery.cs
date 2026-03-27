using Amolenk.Admitto.Module.Organization.Contracts;
using Amolenk.Admitto.Module.Shared.Application.Messaging;

namespace Amolenk.Admitto.Module.Organization.Application.UseCases.Users.GetTeamMembershipRole;

internal sealed record GetTeamMembershipRoleQuery(Guid TeamId, Guid UserId) : Query<TeamMembershipRoleDto?>;