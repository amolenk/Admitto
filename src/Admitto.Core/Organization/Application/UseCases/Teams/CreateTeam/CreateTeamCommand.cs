using Amolenk.Admitto.Core.Shared.Application.Messaging;

namespace Amolenk.Admitto.Core.Organization.Application.UseCases.Teams.CreateTeam;

internal sealed record CreateTeamCommand(
    string Name)
    : Command<Guid>;