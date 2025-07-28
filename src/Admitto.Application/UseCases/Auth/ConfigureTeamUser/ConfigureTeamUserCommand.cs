using Amolenk.Admitto.Domain.ValueObjects;

namespace Amolenk.Admitto.Application.UseCases.Auth.ConfigureTeamUser;

public record ConfigureTeamUserCommand(string Email, string TeamSlug, TeamMemberRole Role) : Command;