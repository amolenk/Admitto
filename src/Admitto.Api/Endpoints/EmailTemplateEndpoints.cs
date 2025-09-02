using Amolenk.Admitto.ApiService.Middleware;
using Amolenk.Admitto.Application.UseCases.EmailTemplates.ClearEventEmailTemplate;
using Amolenk.Admitto.Application.UseCases.EmailTemplates.ClearTeamEmailTemplate;
using Amolenk.Admitto.Application.UseCases.EmailTemplates.GetEventEmailTemplates;
using Amolenk.Admitto.Application.UseCases.EmailTemplates.GetTeamEmailTemplates;
using Amolenk.Admitto.Application.UseCases.EmailTemplates.SetEventEmailTemplate;
using Amolenk.Admitto.Application.UseCases.EmailTemplates.SetTeamEmailTemplate;

namespace Amolenk.Admitto.ApiService.Endpoints;

public static class EmailTemplateEndpoints
{
    public static void MapEmailTemplateEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/teams/{teamSlug}")
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
            .MapSetEventEmailTemplate()
            .MapSetTeamEmailTemplate()
            .MapGetEventEmailTemplates()
            .MapGetTeamEmailTemplates();
    }
}