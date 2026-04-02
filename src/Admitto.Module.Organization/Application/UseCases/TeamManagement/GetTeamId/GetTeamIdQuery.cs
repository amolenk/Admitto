using Amolenk.Admitto.Module.Shared.Application.Messaging;

namespace Amolenk.Admitto.Module.Organization.Application.UseCases.TeamManagement.GetTeamId;

internal record GetTeamIdQuery(string TeamSlug) : Query<Guid>;