using Amolenk.Admitto.Shared.Application.Messaging;
using Amolenk.Admitto.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Organization.Application.UseCases.Teams.CreateTeam;

internal sealed record CreateTeamCommand(
    string Slug,
    string Name,
    string EmailAddress)
    : Command;