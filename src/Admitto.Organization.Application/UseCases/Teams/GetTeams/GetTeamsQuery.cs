using Amolenk.Admitto.Shared.Application.Messaging;

namespace Amolenk.Admitto.Organization.Application.UseCases.Teams.GetTeams;

internal record GetTeamIdQuery(string TeamSlug) : Query<object>;