using Amolenk.Admitto.Core.Shared.Application.Messaging;

namespace Amolenk.Admitto.Core.Organization.Application.UseCases.Teams.UpdateTeam;

internal sealed record UpdateTeamCommand(
    Guid TeamId,
    string? Name,
    uint? ExpectedVersion,
    string? AccentColor = null)
    : Command;
