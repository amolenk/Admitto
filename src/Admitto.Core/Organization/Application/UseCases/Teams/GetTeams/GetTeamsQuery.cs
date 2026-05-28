using Amolenk.Admitto.Core.Shared.Application.Messaging;

namespace Amolenk.Admitto.Core.Organization.Application.UseCases.Teams.GetTeams;

/// <summary>
/// Query to retrieve teams, with results scoped by the caller's role (US-003 / US-006).
/// </summary>
internal sealed record GetTeamsQuery(Guid CallerId, bool CallerIsAdmin)
    : Query<IReadOnlyList<TeamListItemDto>>;
