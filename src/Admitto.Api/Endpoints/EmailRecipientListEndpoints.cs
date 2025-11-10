using Amolenk.Admitto.ApiService.Middleware;
using Amolenk.Admitto.Application.UseCases.EmailRecipientLists.AddEmailRecipientList;
using Amolenk.Admitto.Application.UseCases.EmailRecipientLists.GetEmailRecipientLists;
using Amolenk.Admitto.Application.UseCases.EmailRecipientLists.RemoveEmailRecipientList;

namespace Amolenk.Admitto.ApiService.Endpoints;

public static class EmailRecipientListEndpoints
{
    public static void MapEmailRecipientListEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/teams/{teamSlug}/events/{eventSlug}/email-recipient-lists")
            .WithTags("Email Recipient Lists")
            .AddEndpointFilter<ValidationFilter>()
            .AddEndpointFilter<UnitOfWorkFilter>()
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status500InternalServerError)
            .RequireAuthorization();

        group
            .MapAddEmailRecipientList()
            .MapGetEmailRecipientLists()
            .MapRemoveEmailRecipientList();
    }
}