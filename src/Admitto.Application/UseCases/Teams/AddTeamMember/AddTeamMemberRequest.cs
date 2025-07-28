using Amolenk.Admitto.Domain.ValueObjects;

namespace Amolenk.Admitto.Application.UseCases.Teams.AddTeamMember;

public record AddTeamMemberRequest(string Email, TeamMemberRole Role);