using Amolenk.Admitto.Application.Common.Authorization;
using Amolenk.Admitto.Application.UseCases.Email.SendEmail;
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
            .RequireAuthorization(policy => policy.RequireRebacCheck("can_manage_teams"));
        
        return group;
    }

    private static async Task<Results<Created<CreateTeamResponse>, ValidationProblem, Conflict<HttpValidationProblemDetails>>> CreateTeam(
        CreateTeamRequest request, CreateTeamValidator validator, IDomainContext context, IEmailOutbox emailOutbox,
        IUnitOfWork unitOfWork, CancellationToken cancellationToken)
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
                Title = "Conflict",
                Detail = $"A team with the name '{request.Name}' already exists.",
                Status = StatusCodes.Status409Conflict,
                Errors = {
                    ["name"] = ["Team name must be unique."]
                }
            });
        }
        
        return TypedResults.Created($"/teams/{team.Id}", CreateTeamResponse.FromTeam(team));
    }
}
