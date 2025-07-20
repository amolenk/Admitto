using Amolenk.Admitto.ApiService.Middleware;
using Amolenk.Admitto.Application.UseCases.Email.ClearEventEmailTemplate;
using Amolenk.Admitto.Application.UseCases.Email.ClearTeamEmailTemplate;
using Amolenk.Admitto.Application.UseCases.Email.ConfigureEventEmailTemplate;
using Amolenk.Admitto.Application.UseCases.Email.ConfigureTeamEmailTemplate;
using Amolenk.Admitto.Application.UseCases.Email.GetEventEmailTemplates;
using Amolenk.Admitto.Application.UseCases.Email.GetTeamEmailTemplates;
using Amolenk.Admitto.Application.UseCases.Email.SendEmail;

namespace Amolenk.Admitto.ApiService.Endpoints;

public static class EmailEndpoints
{
    public static void MapEmailTemplateEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/")
            .WithTags("Email Templates")
            .AddEndpointFilter<ValidationFilter>()
            .AddEndpointFilter<UnitOfWorkFilter>()
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status500InternalServerError)
            .RequireAuthorization();

        group
            .MapClearEventEmailTemplate()
            .MapClearTeamEmailTemplate()
            .MapConfigureEventEmailTemplate()
            .MapConfigureTeamEmailTemplate()
            .MapGetEventEmailTemplates()
            .MapGetTeamEmailTemplates()
            .MapSendEmail();
    }
}