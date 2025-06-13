using Amolenk.Admitto.Domain.ValueObjects;

namespace Amolenk.Admitto.Application.UseCases.Auth.ConfigureTeamUser;

public record ConfigureTeamUserCommand(Guid TeamId, string Email, TeamMemberRole Role) : ICommand
{
    public Guid Id { get; } = Guid.NewGuid();
}
