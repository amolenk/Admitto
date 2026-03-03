using Amolenk.Admitto.Shared.Application.Messaging;
using Amolenk.Admitto.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Organization.Application.UseCases.Teams.GetTeamId;

// TODO Domain vs DTO???
internal record GetTeamIdQuery(Slug TeamSlug) : Query<TeamId>;