using Amolenk.Admitto.Core.Organization.Application.Persistence;
using Amolenk.Admitto.Core.Organization.Domain.Entities;
using Amolenk.Admitto.Core.Shared.Application.Messaging;
using Amolenk.Admitto.Core.Shared.Kernel.ErrorHandling;
using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;
using Vogen;

namespace Amolenk.Admitto.Core.Organization.Application.UseCases.TeamManagement.CreateTeam;

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