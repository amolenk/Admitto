namespace Amolenk.Admitto.Application.UseCases.Teams.GetTeams;

public record GetTeamsResponse(TeamDto[] Teams);

public record TeamDto(string Slug, string Name, string Email);
