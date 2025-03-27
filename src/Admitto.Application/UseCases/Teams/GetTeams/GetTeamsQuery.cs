using Amolenk.Admitto.Domain.Entities;

namespace Amolenk.Admitto.Application.UseCases.Teams.GetTeams;

public record GetTeamsQuery;

public record GetTeamsResult(IEnumerable<TeamDto> Teams)
{
    public static GetTeamsResult FromTeams(IEnumerable<Team> teams)
    {
        return new GetTeamsResult(teams.Select(TeamDto.FromTeam));
    }
}

public record TeamDto(Guid Id, string Name)
{
    public static TeamDto FromTeam(Team team)
    {
        return new TeamDto(team.Id, team.Name);
    }
}