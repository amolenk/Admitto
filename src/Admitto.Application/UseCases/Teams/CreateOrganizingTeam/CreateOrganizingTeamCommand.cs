namespace Amolenk.Admitto.Application.UseCases.Teams.CreateOrganizingTeam;

public record CreateOrganizingTeamCommand(string Name) : ICommand
{
    public Guid Id { get; } = Guid.NewGuid();
}

public record CreateOrganizingTeamResult(Guid Id);
