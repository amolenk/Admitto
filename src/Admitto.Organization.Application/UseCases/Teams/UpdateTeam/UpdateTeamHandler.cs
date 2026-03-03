using Amolenk.Admitto.Organization.Application.Persistence;
using Amolenk.Admitto.Organization.Domain.Entities;
using Amolenk.Admitto.Shared.Application.Messaging;

namespace Amolenk.Admitto.Organization.Application.UseCases.Teams.CreateTeam;

internal sealed class CreateTeamHandler(IOrganizationWriteStore writeStore)
    : ICommandHandler<CreateTeamCommand>
{
    public async ValueTask HandleAsync(CreateTeamCommand command, CancellationToken cancellationToken)
    {
        var team = Team.Create(command.Slug, command.Name, command.EmailAddress);

        await writeStore.Teams.AddAsync(team, cancellationToken);
    }
}