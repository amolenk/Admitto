using Amolenk.Admitto.Core.Organization.Application.Persistence;
using Amolenk.Admitto.Core.Organization.Domain.ValueObjects;
using Amolenk.Admitto.Core.Shared.Application.Messaging;
using Amolenk.Admitto.Core.Shared.Application.Persistence;

namespace Amolenk.Admitto.Core.Organization.Application.UseCases.Teams.UpdateTeam;

internal sealed class UpdateTeamHandler(IOrganizationWriteStore writeStore)
    : ICommandHandler<UpdateTeamCommand>
{
    public async ValueTask HandleAsync(UpdateTeamCommand command, CancellationToken cancellationToken)
    {
        var teamId = TeamId.From(command.TeamId);

        var team = await writeStore.Teams.GetAsync(teamId, command.ExpectedVersion, cancellationToken);

        if (command.Name is not null)
        {
            team.ChangeName(TeamName.From(command.Name));
        }

        if (command.AccentColor is not null)
        {
            team.ChangeAccentColor(TeamAccentColor.From(command.AccentColor));
        }
    }
}
