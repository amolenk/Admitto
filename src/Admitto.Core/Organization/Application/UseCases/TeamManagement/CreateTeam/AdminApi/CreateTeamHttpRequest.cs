namespace Amolenk.Admitto.Core.Organization.Application.UseCases.TeamManagement.CreateTeam.AdminApi;

public sealed record CreateTeamHttpRequest(
    string Name)
{
    internal CreateTeamCommand ToCommand() => new(Name);
}