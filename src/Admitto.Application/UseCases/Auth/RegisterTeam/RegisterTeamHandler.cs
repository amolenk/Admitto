namespace Amolenk.Admitto.Application.UseCases.Auth.RegisterTeam;

public class RegisterTeamHandler(IAuthorizationService authorizationService) : ICommandHandler<RegisterTeamCommand>
{
    public async ValueTask HandleAsync(RegisterTeamCommand command, CancellationToken cancellationToken)
    {
        await authorizationService.AddTeamAsync(command.TeamSlug, cancellationToken); 
    }
}
