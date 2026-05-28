namespace Amolenk.Admitto.Core.Organization.Application.UseCases.Teams.CreateTeam.AdminApi;

public sealed record CreateTeamHttpRequest(
    string Name)
{
    internal CreateTeamCommand ToCommand() => new(Name);
}