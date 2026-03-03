using Amolenk.Admitto.Shared.Application.Messaging;

namespace Amolenk.Admitto.Organization.Application.UseCases.Teams.GetTeam;

internal sealed record GetTeamQuery(Guid TeamId) : Query<TeamDto>;