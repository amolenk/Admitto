namespace Amolenk.Admitto.Core.Organization.Application.UseCases.Teams.CreateTeam.AdminApi;

public sealed record CreateTeamHttpRequest(
    string Name,
    string? AccentColor = null)
{
    internal CreateTeamCommand ToCommand() => new(Name, AccentColor);
}
