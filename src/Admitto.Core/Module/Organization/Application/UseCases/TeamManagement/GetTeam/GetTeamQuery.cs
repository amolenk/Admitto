using Amolenk.Admitto.Core.Shared.Application.Messaging;

namespace Amolenk.Admitto.Core.Module.Organization.Application.UseCases.TeamManagement.GetTeam;

internal sealed record GetTeamQuery(Guid TeamId) : Query<TeamDto>;