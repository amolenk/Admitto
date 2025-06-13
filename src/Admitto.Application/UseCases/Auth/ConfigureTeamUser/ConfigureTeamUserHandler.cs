using Amolenk.Admitto.Application.UseCases.Auth.AssignTeamRole;

namespace Amolenk.Admitto.Application.UseCases.Auth.ConfigureTeamUser;

/// <summary>
/// Configures a user for a team. Creates the user if it doesn't exist yet and assigns the team role.
/// </summary>
public class ConfigureTeamUserHandler(IIdentityService identityService, IMessageOutbox messageOutbox, 
    ILogger<ConfigureTeamUserHandler> logger)
    : ICommandHandler<ConfigureTeamUserCommand>
{
    public async ValueTask HandleAsync(ConfigureTeamUserCommand command, CancellationToken cancellationToken)
    {
        var user = await identityService.GetUserByEmailAsync(command.Email, cancellationToken);
        if (user is null)
        {
            logger.LogInformation("Adding new user {email} to team {team} with role {role}.", command.Email, 
                command.TeamId, command.Role);
            
            user = await identityService.AddUserAsync(command.Email, cancellationToken);
        }
        
        // Add a command to the outbox to assign the role.
        messageOutbox.Enqueue(new AssignTeamRoleCommand(user.Id, command.TeamId, command.Role));
    }
}
