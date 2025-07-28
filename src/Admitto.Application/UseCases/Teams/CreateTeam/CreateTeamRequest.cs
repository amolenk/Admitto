namespace Amolenk.Admitto.Application.UseCases.Teams.CreateTeam;

public record CreateTeamRequest(string Slug, string Name, string Email, string EmailServiceConnectionString);

