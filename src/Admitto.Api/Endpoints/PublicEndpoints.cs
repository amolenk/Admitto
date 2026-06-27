using Amolenk.Admitto.ApiService.Middleware;
using Amolenk.Admitto.Api.Auth;
using Amolenk.Admitto.Core.Registrations;

namespace Amolenk.Admitto.Api.Endpoints;

public static class PublicEndpoints
{
    public static void MapPublicEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGroup("/api")
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status429TooManyRequests)
            .ProducesProblem(StatusCodes.Status500InternalServerError)
            .AddEndpointFilter<ValidationFilter>()
            .ProducesValidationProblem()
            .RequireAuthorization(policy =>
                policy.AddAuthenticationSchemes(ApiKeyAuthenticationHandler.SchemeName)
                      .RequireAuthenticatedUser())
            .RequireRateLimiting("public-standard")
            .MapRegistrationsPublicEndpoints();

        app.MapGroup(string.Empty)
            .RequireRateLimiting("public-standard")
            .MapRegistrationsPublicEventLinkEndpoints();
    }
}
