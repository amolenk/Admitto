using Amolenk.Admitto.Shared.Application.Messaging;

namespace Amolenk.Admitto.Organization.Application.UseCases.Teams.GetTeamId;

internal record GetTeamIdQuery(string TeamSlug) : Query<Guid>;