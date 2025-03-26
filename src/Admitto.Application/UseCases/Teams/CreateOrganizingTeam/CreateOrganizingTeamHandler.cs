using Amolenk.Admitto.Domain.Entities;

namespace Amolenk.Admitto.Application.UseCases.Teams.CreateOrganizingTeam;

/// <summary>
/// Create a new organizing team.
/// </summary>
public class CreateOrganizingTeamHandler(IDomainContext context) 
    : ICommandHandler<CreateOrganizingTeamCommand, CreateOrganizingTeamResult>
{
    public ValueTask<CreateOrganizingTeamResult> HandleAsync(CreateOrganizingTeamCommand command, 
        CancellationToken cancellationToken)
    {
        var organizingTeam = OrganizingTeam.Create(command.Name);

        context.OrganizingTeams.Add(organizingTeam);
        
        return ValueTask.FromResult(new CreateOrganizingTeamResult(organizingTeam.Id));
    }
}
