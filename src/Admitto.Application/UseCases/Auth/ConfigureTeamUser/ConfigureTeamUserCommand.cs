using Amolenk.Admitto.Domain.ValueObjects;

namespace Amolenk.Admitto.Application.UseCases.Auth.ConfigureTeamUser;

public record ConfigureTeamUserCommand(string Email, Guid TeamId, TeamMemberRole Role) : Command;