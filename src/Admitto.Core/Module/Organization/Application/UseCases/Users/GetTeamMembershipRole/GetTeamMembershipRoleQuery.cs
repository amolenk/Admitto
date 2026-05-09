using Amolenk.Admitto.Core.Module.Organization.Contracts;
using Amolenk.Admitto.Core.Shared.Application.Messaging;

namespace Amolenk.Admitto.Core.Module.Organization.Application.UseCases.Users.GetTeamMembershipRole;

internal sealed record GetTeamMembershipRoleQuery(Guid TeamId, Guid UserId) : Query<TeamMembershipRoleDto?>;