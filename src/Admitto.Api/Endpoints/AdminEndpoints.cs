using Amolenk.Admitto.ApiService.Middleware;
using Amolenk.Admitto.Organization.Application;
using Amolenk.Admitto.Registrations.Application;

namespace Amolenk.Admitto.Api.Endpoints;

public static class AdminEndpoints
{
    public static void MapAdminEndpoints(this IEndpointRouteBuilder app)
    {
        var adminGroup = app.MapGroup("/admin")
            .AddEndpointFilter<ValidationFilter>()
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status500InternalServerError)
            .RequireAuthorization();

        adminGroup
            .MapOrganizationAdminEndpoints()
            .MapRegistrationsAdminEndpoints();
    }
}