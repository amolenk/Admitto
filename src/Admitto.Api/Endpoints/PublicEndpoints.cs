using Amolenk.Admitto.Api.Auth;
using Amolenk.Admitto.ApiService.Middleware;
using Amolenk.Admitto.Module.Registrations.Application;

namespace Amolenk.Admitto.Api.Endpoints;

public static class PublicEndpoints
{
    public static void MapPublicEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGroup("/api")
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status500InternalServerError)
            .AddEndpointFilter<ValidationFilter>()
            .AddEndpointFilter<ApiKeyTeamScopeFilter>()
            .ProducesValidationProblem()
            .RequireAuthorization(policy =>
                policy.AddAuthenticationSchemes(ApiKeyAuthenticationHandler.SchemeName)
                      .RequireAuthenticatedUser())
            .MapRegistrationsPublicEndpoints();
    }
}
