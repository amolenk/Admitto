using Amolenk.Admitto.Module.Shared.Application.Messaging;

namespace Amolenk.Admitto.Module.Organization.Application.UseCases.TeamManagement.UpdateTeam;

internal sealed record UpdateTeamCommand(
    Guid TeamId,
    string? Name,
    string? EmailAddress,
    uint? ExpectedVersion)
    : Command;