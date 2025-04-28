using Amolenk.Admitto.Domain.Entities;

namespace Amolenk.Admitto.Application.UseCases.Teams.CreateTeam;

public record CreateTeamResponse(Guid Id)
{
    public static CreateTeamResponse FromTeam(Team team)
    {
        return new CreateTeamResponse(team.Id);
    }
}

