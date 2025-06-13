namespace Amolenk.Admitto.Application.UseCases.Auth.AssignTeamRole;

/// <summary>
/// Assigns a team role to a user.
/// </summary>
public class AssignTeamRoleHandler()
    : ICommandHandler<AssignTeamRoleCommand>
{
    public ValueTask HandleAsync(AssignTeamRoleCommand command, CancellationToken cancellationToken)
    {
        // TODO
        Console.WriteLine($"Assign team role {command.Role} to user {command.UserId} in team {command.TeamId}");
        
        return ValueTask.CompletedTask;
    }
}
