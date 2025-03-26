using Amolenk.Admitto.Domain.Entities;

namespace Amolenk.Admitto.Application.UseCases.Teams.AddTeamMember;

public record AddTeamMemberCommand(Guid OrganizingTeamId, string Email, UserRole Role) : ICommand
{
    public Guid Id { get; init; } = Guid.NewGuid();
}
