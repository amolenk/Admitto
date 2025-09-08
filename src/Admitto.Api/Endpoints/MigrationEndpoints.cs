using Amolenk.Admitto.Application.UseCases.Migration.GetMigrations;
using Amolenk.Admitto.Application.UseCases.Migration.RunMigration;

namespace Amolenk.Admitto.ApiService.Endpoints;

public static class MigrationEndpoints
{
    public static void MapMigrationEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/migration")
            .WithTags("Migration")
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status500InternalServerError)
            .RequireAuthorization();

        group
            .MapGetMigrations()
            .MapRunMigration();
    }
}
