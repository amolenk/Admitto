using Amolenk.Admitto.Domain.Entities;

namespace Amolenk.Admitto.Application.UseCases.Teams.AddTeamMember;

/// <summary>
/// Add a new user to an organizing team.
/// </summary>
public class AddTeamMemberHandler(IDomainContext context) : ICommandHandler<AddTeamMemberCommand, Guid>
{
    public async ValueTask<Result<Guid>> HandleAsync(AddTeamMemberCommand command, CancellationToken cancellationToken)
    {
        var team = await context.Teams.FindAsync([command.OrganizingTeamId], cancellationToken);
        if (team is null)
        {
            return Result<Guid>.Failure("Team not found.");
        }

        var user = User.Create(command.Email, command.Role);
        
        team.AddMember(User.Create(command.Email, command.Role));
        
        return Result<Guid>.Success(user.Id);
    }
}
