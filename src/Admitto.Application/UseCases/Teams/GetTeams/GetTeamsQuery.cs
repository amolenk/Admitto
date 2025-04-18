using Amolenk.Admitto.Domain.Entities;

namespace Amolenk.Admitto.Application.UseCases.Teams.GetTeams;

public record GetTeamsQuery;

public record TeamDto(Guid Id, string Name)
{
    public static TeamDto FromTeam(Team team)
    {
        return new TeamDto(team.Id, team.Name);
    }
}