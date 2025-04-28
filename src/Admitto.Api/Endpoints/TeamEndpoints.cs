using Amolenk.Admitto.Application.UseCases.Teams.AddTeamMember;
using Amolenk.Admitto.Application.UseCases.Teams.CreateTeam;
using Amolenk.Admitto.Application.UseCases.Teams.GetTeams;
using Amolenk.Admitto.Domain.Entities;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace Amolenk.Admitto.ApiService.Endpoints;

public static class TeamEndpoints
{
    public static void MapTeamEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/teams").WithTags("Teams");

        group
            .MapCreateTeam()
            .MapAddTeamMember()
            .MapGetTeams();
    }
}
