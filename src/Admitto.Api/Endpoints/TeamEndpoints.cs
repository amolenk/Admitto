using Amolenk.Admitto.Application.UseCases.Teams.AddTeam;
using Amolenk.Admitto.Application.UseCases.Teams.AddTeamMember;
using Amolenk.Admitto.Application.UseCases.Teams.GetTeams;
using Amolenk.Admitto.Domain.Entities;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace Amolenk.Admitto.ApiService.Endpoints;

public record AddTeamMemberRequest(string Email, UserRole Role)
{
    public Guid Id { get; } = Guid.NewGuid();
}

public static class TeamEndpoints
{
    public static void MapTeamEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/teams").WithTags("Teams");

        group.MapGet("/", GetTeams)
            .WithName(nameof(GetTeams));

        group.MapPost("/", AddTeam)
            .WithName(nameof(AddTeam))
            .Produces(StatusCodes.Status201Created)
            .ProducesValidationProblem();

        group.MapPost("/{teamId:guid}/members", AddTeamMember)
            .WithName(nameof(AddTeamMember))
            .Produces(StatusCodes.Status201Created)
            .ProducesValidationProblem();
    }

    private static async Task<Ok<IEnumerable<TeamDto>>> GetTeams(GetTeamsHandler handler)
    {
        var query = new GetTeamsQuery();
        
        var result = await handler.HandleAsync(query, CancellationToken.None);
        return TypedResults.Ok(result.Value);
    }
    
    private static async Task<Results<Created<Guid>, ValidationProblem>> AddTeam(
        [FromBody] AddTeamCommand command, [FromServices] AddTeamHandler handler)
    {
        var result = await handler.HandleAsync(command, CancellationToken.None);

        return TypedResults.Created($"/teams/{result.Value}", result.Value);
    }
    
    private static async Task<Results<Created, ValidationProblem>> AddTeamMember(Guid teamId,
        [FromBody] AddTeamMemberRequest request, [FromServices] AddTeamMemberHandler handler)
    {
        var command = new AddTeamMemberCommand(teamId, request.Email, request.Role)
        {
            Id = request.Id
        };
        
        await handler.HandleAsync(command, CancellationToken.None);

        return TypedResults.Created();
    }
}
