using Amolenk.Admitto.Core.Shared.Application.Messaging;

namespace Amolenk.Admitto.Core.Organization.Application.UseCases.TeamManagement.UpdateTeam;

internal sealed record UpdateTeamCommand(
    Guid TeamId,
    string? Name,
    string? EmailAddress,
    uint? ExpectedVersion)
    : Command;