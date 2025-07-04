using Amolenk.Admitto.Application.Common.Authorization;
using Amolenk.Admitto.Domain;
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
            .RequireAuthorization(policy => policy.RequireRebacCheck(Permission.CanManageTeams));
        
        return group;
    }

    private static async Task<Results<Created<CreateTeamResponse>, ValidationProblem, Conflict<HttpValidationProblemDetails>>> CreateTeam(
        CreateTeamRequest request, CreateTeamValidator validator, IDomainContext context, IUnitOfWork unitOfWork, 
        CancellationToken cancellationToken)
    {
        await validator.ValidateAndThrowAsync(request, cancellationToken);

        var emailSettings = new EmailSettings(
            request.EmailSettings.SenderEmail,
            request.EmailSettings.SmtpServer,
            request.EmailSettings.SmtpPort);
        
        var team = Team.Create(request.Name, emailSettings);
        
        foreach (var member in request.Members)
        {
            team.AddMember(member.Email, member.Role);
        }
        
        context.Teams.Add(team);
        
        try
        {        
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            return TypedResults.Conflict(new HttpValidationProblemDetails
            {
                Detail = ErrorMessage.Team.AlreadyExists,
                Status = StatusCodes.Status409Conflict,
                Errors = {
                    [nameof(request.Name)] = [ErrorMessage.Team.Name.MustBeUnique]
                }
            });
        }
        
        return TypedResults.Created($"/teams/{team.Id}", CreateTeamResponse.FromTeam(team));
    }
}
