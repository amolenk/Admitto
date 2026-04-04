using Amolenk.Admitto.ApiService.Middleware;
using Amolenk.Admitto.Module.Registrations.Application;

namespace Amolenk.Admitto.Api.Endpoints;

public static class PublicEndpoints
{
    public static void MapPublicEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGroup("")
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status500InternalServerError)
            .AddEndpointFilter<ValidationFilter>()
            .ProducesValidationProblem()
            .MapRegistrationsPublicEndpoints();
    }
}
