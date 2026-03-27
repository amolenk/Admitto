using Amolenk.Admitto.Module.Shared.Application.Messaging;

namespace Amolenk.Admitto.Module.Organization.Application.UseCases.Teams.GetTeamId;

internal record GetTeamIdQuery(string TeamSlug) : Query<Guid>;