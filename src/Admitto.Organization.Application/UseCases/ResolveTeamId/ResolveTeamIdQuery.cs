using Amolenk.Admitto.Organization.Domain.ValueObjects;
using Amolenk.Admitto.Shared.Application.Messaging;

namespace Amolenk.Admitto.Organization.Application.UseCases.ResolveTeamId;

internal record ResolveTeamIdQuery(TeamSlug TeamSlug) : Query<Guid>;