using Amolenk.Admitto.Domain.ValueObjects;

namespace Amolenk.Admitto.Application.UseCases.Teams.AddTeamMember;

public record AddTeamMemberRequest(string Email, string FirstName, string LastName, TeamMemberRole Role);