using Amolenk.Admitto.Module.Shared.Application.Messaging;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Module.Organization.Application.UseCases.Teams.UpdateTeam;

internal sealed record UpdateTeamCommand(
    Guid TeamId,
    uint ExpectedVersion,
    string? Slug,
    string? Name,
    string? EmailAddress)
    : Command;