using Amolenk.Admitto.Core.Organization.Application.Persistence;
using Amolenk.Admitto.Core.Organization.Domain.Entities;
using Amolenk.Admitto.Core.Shared.Application.Messaging;
using Amolenk.Admitto.Core.Shared.Kernel.ErrorHandling;
using Vogen;

namespace Amolenk.Admitto.Core.Organization.Application.UseCases.Teams.CreateTeam;

internal sealed class CreateTeamHandler(IOrganizationWriteStore writeStore)
    : ICommandHandler<CreateTeamCommand, Guid>
{
    public async ValueTask<Guid> HandleAsync(CreateTeamCommand command, CancellationToken cancellationToken)
    {
        TeamName name;
        try
        {
            name = TeamName.From(command.Name);
        }
        catch (ValueObjectValidationException)
        {
            throw new BusinessRuleViolationException(CommonErrors.TextEmpty);
        }

        var team = Team.Create(name);

        await writeStore.Teams.AddAsync(team, cancellationToken);

        return team.Id.Value;
    }
}