using Amolenk.Admitto.Domain.Entities;

namespace Amolenk.Admitto.Application.UseCases.Teams.AddTeam;

/// <summary>
/// Add a team for organizing events.
/// </summary>
public class AddTeamHandler(IDomainContext context) : ICommandHandler<AddTeamCommand, Guid>
{
    public ValueTask<Result<Guid>> HandleAsync(AddTeamCommand command, CancellationToken cancellationToken)
    {
        var team = Team.Create(command.Name);
        
        context.Teams.Add(team);

        return ValueTask.FromResult(Result<Guid>.Success(team.Id));
    }
}
