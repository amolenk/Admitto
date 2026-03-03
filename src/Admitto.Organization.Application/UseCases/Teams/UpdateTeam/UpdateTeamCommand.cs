using Amolenk.Admitto.Shared.Application.Messaging;
using Amolenk.Admitto.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Organization.Application.UseCases.Teams.UpdateTeam;

internal sealed record UpdateTeamCommand(
    Guid TeamId,
    uint ExpectedVersion,
    string? Slug,
    string? Name,
    string? EmailAddress)
    : Command;