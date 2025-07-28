namespace Amolenk.Admitto.Application.UseCases.Auth.RegisterTeam;

/// <summary>
/// Registers a team with the authorization service.
/// </summary>
public class RegisterTeamHandler(IAuthorizationService authorizationService, ILogger<RegisterTeamHandler> logger)
    : ICommandHandler<RegisterTeamCommand>
{
    public async ValueTask HandleAsync(RegisterTeamCommand command, CancellationToken cancellationToken)
    {
        await authorizationService.AddTeamAsync(command.TeamSlug, cancellationToken);
        
        logger.LogInformation(
            "Registered team '{team}' with the authorization service.",
            command.TeamSlug);
    }
}
