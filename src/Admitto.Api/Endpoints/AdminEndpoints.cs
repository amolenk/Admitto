using Amolenk.Admitto.ApiService.Middleware;
using Amolenk.Admitto.Core.Badges;
using Amolenk.Admitto.Core.Email;
using Amolenk.Admitto.Core.Organization;
using Amolenk.Admitto.Core.Registrations;

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
            .MapEmailAdminEndpoints()
            .MapOrganizationAdminEndpoints()
            .MapRegistrationsAdminEndpoints()
            .MapBadgesAdminEndpoints();
    }
}
