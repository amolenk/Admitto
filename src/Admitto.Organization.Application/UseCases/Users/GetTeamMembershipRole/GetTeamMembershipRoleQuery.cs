using Amolenk.Admitto.Organization.Domain.ValueObjects;
using Amolenk.Admitto.Shared.Application.Messaging;
using Amolenk.Admitto.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Organization.Application.UseCases.Users.GetTeamMembershipRole;

internal sealed record GetTeamMembershipRoleQuery(TeamId TeamId, UserId UserId) : Query<TeamMembershipRole?>;