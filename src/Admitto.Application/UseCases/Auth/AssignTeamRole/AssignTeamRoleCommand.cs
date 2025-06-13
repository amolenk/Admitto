using Amolenk.Admitto.Domain.ValueObjects;

namespace Amolenk.Admitto.Application.UseCases.Auth.AssignTeamRole;

public record AssignTeamRoleCommand(Guid UserId, Guid TeamId, TeamMemberRole Role) : ICommand
{
    public Guid Id { get; } = Guid.NewGuid();
}
