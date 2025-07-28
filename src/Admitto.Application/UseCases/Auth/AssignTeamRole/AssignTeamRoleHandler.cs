namespace Amolenk.Admitto.Application.UseCases.Auth.AssignTeamRole;

/// <summary>
/// Assigns a team role to a user.
/// </summary>
public class AssignTeamRoleHandler(IAuthorizationService authorizationService)
    : ICommandHandler<AssignTeamRoleCommand>
{
    public async ValueTask HandleAsync(AssignTeamRoleCommand command, CancellationToken cancellationToken)
    {
        await authorizationService.AddTeamRoleAsync(command.UserId, command.TeamSlug, command.Role, cancellationToken);
    }
}
