namespace Amolenk.Admitto.Application.UseCases.Auth.RegisterTeam;

public record RegisterTeamCommand(string TeamSlug) : Command
{
    public Guid Id { get; } = Guid.NewGuid();
}
