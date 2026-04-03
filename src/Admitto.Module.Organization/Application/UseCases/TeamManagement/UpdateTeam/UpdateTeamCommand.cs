using Amolenk.Admitto.Module.Shared.Application.Messaging;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Module.Organization.Application.UseCases.TeamManagement.UpdateTeam;

internal sealed record UpdateTeamCommand(
    Guid TeamId,
    string? Slug,
    string? Name,
    string? EmailAddress,
    uint? ExpectedVersion)
    : Command;