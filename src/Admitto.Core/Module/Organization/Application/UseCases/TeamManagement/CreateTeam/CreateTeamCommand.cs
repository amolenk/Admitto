using Amolenk.Admitto.Core.Shared.Application.Messaging;
using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Core.Module.Organization.Application.UseCases.TeamManagement.CreateTeam;

internal sealed record CreateTeamCommand(
    string Name,
    string EmailAddress)
    : Command;