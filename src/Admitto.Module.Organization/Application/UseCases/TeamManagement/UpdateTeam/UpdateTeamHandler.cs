using Amolenk.Admitto.Module.Organization.Application.Persistence;
using Amolenk.Admitto.Module.Shared.Application.Messaging;
using Amolenk.Admitto.Module.Shared.Application.Persistence;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Module.Organization.Application.UseCases.TeamManagement.UpdateTeam;

internal sealed class UpdateTeamHandler(IOrganizationWriteStore writeStore)
    : ICommandHandler<UpdateTeamCommand>
{
    public async ValueTask HandleAsync(UpdateTeamCommand command, CancellationToken cancellationToken)
    {
        var team = await writeStore.Teams.GetAsync(
            TeamId.From(command.TeamId),
            command.ExpectedVersion,
            cancellationToken);

        if (command.Name is not null)
        {
            team.ChangeName(DisplayName.From(command.Name));
        }

        if (command.EmailAddress is not null)
        {
            team.ChangeEmailAddress(EmailAddress.From(command.EmailAddress));
        }
    }
}