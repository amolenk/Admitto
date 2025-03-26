using Amolenk.Admitto.Domain.Entities;

namespace Amolenk.Admitto.Application.UseCases.Teams.AddTeam;

/// <summary>
/// Add a team for organizing events.
/// </summary>
public class AddTeamHandler(IDomainContext context) : ICommandHandler<AddTeamCommand, AddTeamResult>
{
    public ValueTask<AddTeamResult> HandleAsync(AddTeamCommand command, CancellationToken cancellationToken)
    {
        var team = OrganizingTeam.Create(command.Name);
        
        context.OrganizingTeams.Add(team);

        return ValueTask.FromResult(new AddTeamResult(team.Id));
    }
}
