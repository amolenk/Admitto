using Amolenk.Admitto.ApiService.Middleware;
using Amolenk.Admitto.Application.UseCases.BulkEmail.SendCustomBulkEmail;
using Amolenk.Admitto.Application.UseCases.BulkEmail.SendReconfirmBulkEmail;

namespace Amolenk.Admitto.ApiService.Endpoints;

public static class BulkEmailEndpoints
{
    public static void MapBulkEmailEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/teams/{teamSlug}/events/{eventSlug}/emails/bulk")
            .WithTags("Bulk Email")
            .AddEndpointFilter<ValidationFilter>()
            .AddEndpointFilter<UnitOfWorkFilter>()
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status500InternalServerError)
            .RequireAuthorization();

        group
            .MapSendCustomBulkEmail()
            .MapSendReconfirmBulkEmail();
    }
}