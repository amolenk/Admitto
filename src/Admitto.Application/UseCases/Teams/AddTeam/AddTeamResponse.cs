using Amolenk.Admitto.Domain.Entities;

namespace Amolenk.Admitto.Application.UseCases.Teams.AddTeam;

public record AddTeamResponse(Guid Id)
{
    public static AddTeamResponse FromTeam(Team team)
    {
        return new AddTeamResponse(team.Id);
    }
}

