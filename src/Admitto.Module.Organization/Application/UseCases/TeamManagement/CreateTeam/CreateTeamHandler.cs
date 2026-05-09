using Amolenk.Admitto.Module.Organization.Application.Persistence;
using Amolenk.Admitto.Module.Organization.Domain.Entities;
using Amolenk.Admitto.Module.Shared.Application.Messaging;
using Amolenk.Admitto.Module.Shared.Kernel.ErrorHandling;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;
using Vogen;

namespace Amolenk.Admitto.Module.Organization.Application.UseCases.TeamManagement.CreateTeam;

internal sealed class CreateTeamHandler(IOrganizationWriteStore writeStore)
    : ICommandHandler<CreateTeamCommand>
{
    public async ValueTask HandleAsync(CreateTeamCommand command, CancellationToken cancellationToken)
    {
        DisplayName name;
        try
        {
            name = DisplayName.From(command.Name);
        }
        catch (ValueObjectValidationException)
        {
            throw new BusinessRuleViolationException(CommonErrors.TextEmpty);
        }

        var emailAddress = EmailAddress.From(command.EmailAddress);
        
        var team = Team.Create(name, emailAddress);

        await writeStore.Teams.AddAsync(team, cancellationToken);
    }
}