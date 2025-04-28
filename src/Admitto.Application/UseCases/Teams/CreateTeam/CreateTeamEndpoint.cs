using Amolenk.Admitto.Domain.Entities;
using FluentValidation.Results;

namespace Amolenk.Admitto.Application.UseCases.Teams.CreateTeam;

/// <summary>
/// Add a team for organizing events.
/// </summary>
public static class CreateTeamEndpoint
{
    public static RouteGroupBuilder MapCreateTeam(this RouteGroupBuilder group)
    {
        group.MapPost("/", CreateTeam);
        return group;
    }

    private static async Task<Created<CreateTeamResponse>> CreateTeam(CreateTeamRequest request, 
        CreateTeamValidator validator, IDomainContext context, IUnitOfWork unitOfWork,
        CancellationToken cancellationToken)
    {
        await validator.ValidateAndThrowAsync(request, cancellationToken);

        var team = Team.Create(request.Name);
        
        context.Teams.Add(team);

        try
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            throw new ValidationException($"A team with the name '{request.Name}' already exists.",
                [new ValidationFailure("Name", "Team name must be unique.")]);
        }
        
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return TypedResults.Created($"/teams/{team.Id}", CreateTeamResponse.FromTeam(team));
    }
}
