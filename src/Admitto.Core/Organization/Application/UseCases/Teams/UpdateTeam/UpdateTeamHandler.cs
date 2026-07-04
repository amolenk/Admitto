using Amolenk.Admitto.Core.Organization.Application.Persistence;
using Amolenk.Admitto.Core.Organization.Domain.ValueObjects;
using Amolenk.Admitto.Core.Shared.Application.Messaging;
using Amolenk.Admitto.Core.Shared.Application.Persistence;
using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Core.Organization.Application.UseCases.Teams.UpdateTeam;

internal sealed class UpdateTeamHandler(IOrganizationWriteStore writeStore)
    : ICommandHandler<UpdateTeamCommand>
{
    public async ValueTask HandleAsync(UpdateTeamCommand command, CancellationToken cancellationToken)
    {
        var teamId = TeamId.From(command.TeamId);

        var team = await writeStore.Teams.GetAsync(teamId, command.ExpectedVersion, cancellationToken);

        team.UpdateDetails(
            command.Name is null ? null : TeamName.From(command.Name),
            command.AccentColor is null ? null : TeamAccentColor.From(command.AccentColor),
            command.ReplyToEmailAddress is not null || command.ClearReplyToEmailAddress,
            command.ReplyToEmailAddress is null ? null : EmailAddress.From(command.ReplyToEmailAddress));
    }
}
