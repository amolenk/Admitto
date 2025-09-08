using Amolenk.Admitto.Application.Common.Cryptography;
using Amolenk.Admitto.Domain.Entities;
using Amolenk.Admitto.Domain.ValueObjects;

namespace Amolenk.Admitto.Application.UseCases.Teams.CreateTeam;

/// <summary>
/// Add a team for organizing events.
/// </summary>
public static class CreateTeamEndpoint
{
    public static RouteGroupBuilder MapCreateTeam(this RouteGroupBuilder group)
    {
        group
            .MapPost("/", CreateTeam)
            .WithName(nameof(CreateTeam))
            .RequireAuthorization(policy => policy.RequireAdmin());
        
        return group;
    }

    private static Created CreateTeam(
        CreateTeamRequest request,
        IApplicationContext context,
        CancellationToken cancellationToken)
    {
        var team = Team.Create(request.Slug, request.Name, request.Email, request.EmailServiceConnectionString);
        
        context.Teams.Add(team);

        return TypedResults.Created();
    }
}
