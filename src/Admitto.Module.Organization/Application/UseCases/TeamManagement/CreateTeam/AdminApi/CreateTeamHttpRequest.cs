namespace Amolenk.Admitto.Module.Organization.Application.UseCases.TeamManagement.CreateTeam.AdminApi;

public sealed record CreateTeamHttpRequest(
    string Slug,
    string Name,
    string EmailAddress)
{
    internal CreateTeamCommand ToCommand() => new(Slug, Name, EmailAddress);
}