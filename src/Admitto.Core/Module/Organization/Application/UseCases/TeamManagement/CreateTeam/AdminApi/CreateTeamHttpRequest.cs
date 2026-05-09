namespace Amolenk.Admitto.Core.Module.Organization.Application.UseCases.TeamManagement.CreateTeam.AdminApi;

public sealed record CreateTeamHttpRequest(
    string Name,
    string EmailAddress)
{
    internal CreateTeamCommand ToCommand() => new(Name, EmailAddress);
}