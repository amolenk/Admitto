using Amolenk.Admitto.Domain.ValueObjects;

namespace Amolenk.Admitto.Application.UseCases.Auth.ConfigureTeamUser;

public record ConfigureTeamUserCommand(Guid TeamId, string TeamSlug, string Email, TeamMemberRole Role) : Command
{
    public Guid Id { get; } = Guid.NewGuid();
}
