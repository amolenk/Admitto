namespace Amolenk.Admitto.Module.Organization.Application.UseCases.Teams.CreateTeam.AdminApi;

public sealed record CreateTeamHttpRequest(
    string Slug,
    string Name,
    string EmailAddress)
{
    internal CreateTeamCommand ToCommand() => new(Slug, Name, EmailAddress);
}