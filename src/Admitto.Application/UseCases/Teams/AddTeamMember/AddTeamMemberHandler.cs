using Amolenk.Admitto.Domain.Entities;

namespace Amolenk.Admitto.Application.UseCases.Teams.AddTeamMember;

/// <summary>
/// Add a new user to an organizing team.
/// </summary>
public class AddTeamMemberHandler(IDomainContext context) : ICommandHandler<AddTeamMemberCommand>
{
    public async ValueTask HandleAsync(AddTeamMemberCommand command, CancellationToken cancellationToken)
    {
        var team = await context.OrganizingTeams.GetByIdAsync(command.OrganizingTeamId, cancellationToken);
        
        team.AddMember(User.Create(command.Email, command.Role));
    }
}
