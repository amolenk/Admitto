namespace Amolenk.Admitto.Application.UseCases.Teams.AddTeam;

public record AddTeamCommand(string Name) : ICommand
{
    public Guid Id { get; init; } = Guid.NewGuid();
}

public record AddTeamResult(Guid Id);
