namespace Amolenk.Admitto.Core.Organization.Application.UseCases.TeamManagement.CreateTeam.AdminApi;

public sealed record CreateTeamHttpRequest(
    string Name,
    string EmailAddress)
{
    internal CreateTeamCommand ToCommand() => new(Name, EmailAddress);
}