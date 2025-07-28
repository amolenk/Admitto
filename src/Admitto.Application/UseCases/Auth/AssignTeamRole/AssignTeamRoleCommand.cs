using Amolenk.Admitto.Domain.ValueObjects;

namespace Amolenk.Admitto.Application.UseCases.Auth.AssignTeamRole;

public record AssignTeamRoleCommand(Guid UserId, string TeamSlug, TeamMemberRole Role) : Command;
