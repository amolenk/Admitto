using Amolenk.Admitto.Module.Shared.Application.Messaging;

namespace Amolenk.Admitto.Module.Organization.Application.UseCases.Teams.GetTeam;

internal sealed record GetTeamQuery(Guid TeamId) : Query<TeamDto>;