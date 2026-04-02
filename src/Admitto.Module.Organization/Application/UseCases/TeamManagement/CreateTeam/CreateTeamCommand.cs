using Amolenk.Admitto.Module.Shared.Application.Messaging;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Module.Organization.Application.UseCases.TeamManagement.CreateTeam;

internal sealed record CreateTeamCommand(
    string Slug,
    string Name,
    string EmailAddress)
    : Command;