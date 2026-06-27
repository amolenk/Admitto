using Amolenk.Admitto.Core.Registrations;

namespace Amolenk.Admitto.Api.Endpoints;

public static class PublicEndpoints
{
    public static void MapPublicEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGroup(string.Empty)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status429TooManyRequests)
            .ProducesProblem(StatusCodes.Status500InternalServerError)
            .RequireRateLimiting("public-standard")
            .MapRegistrationsPublicEndpoints();
    }
}
