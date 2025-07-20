using Amolenk.Admitto.Domain.ValueObjects;

namespace Amolenk.Admitto.Application.UseCases.Auth.AssignTeamRole;

public record AssignTeamRoleCommand(Guid UserId, Guid TeamId, string TeamSlug, TeamMemberRole Role) : Command
{
    public Guid Id { get; } = Guid.NewGuid();
}
