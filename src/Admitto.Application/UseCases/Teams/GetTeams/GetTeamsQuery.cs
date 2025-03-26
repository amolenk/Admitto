using Amolenk.Admitto.Domain.Entities;

namespace Amolenk.Admitto.Application.UseCases.Teams.GetTeams;

public record GetTeamsQuery;

public record GetTeamsResult(IEnumerable<TeamDto> Teams)
{
    public static GetTeamsResult FromTeams(IEnumerable<OrganizingTeam> teams)
    {
        return new GetTeamsResult(teams.Select(TeamDto.FromTeam));
    }
}

public record TeamDto(string Name)
{
    public static TeamDto FromTeam(OrganizingTeam team)
    {
        return new TeamDto(team.Name);
    }
}