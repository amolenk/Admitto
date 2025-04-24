using Amolenk.Admitto.Domain.Entities;

namespace Amolenk.Admitto.Application.UseCases.Teams.GetTeams;

public record GetTeamsResponse(TeamDto[] Teams)
{
    public static GetTeamsResponse FromTeams(IEnumerable<Team> teams)
    {
        return new GetTeamsResponse(teams.Select(TeamDto.FromTeam).ToArray());
    }
}

public record TeamDto(Guid Id, string Name)
{
    public static TeamDto FromTeam(Team team)
    {
        return new TeamDto(team.Id, team.Name);
    }
}