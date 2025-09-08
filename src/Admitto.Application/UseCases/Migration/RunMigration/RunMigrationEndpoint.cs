namespace Amolenk.Admitto.Application.UseCases.Migration.RunMigration;

public static class RunMigrationEndpoint
{
    public static RouteGroupBuilder MapRunMigration(this RouteGroupBuilder group)
    {
        group
            .MapPost("/{migrationName}", RunMigration)
            .WithName(nameof(RunMigration))
            .RequireAuthorization(policy => policy.RequireAdmin());
        
        return group;
    }

    // TODO
    private static async ValueTask<Results<Ok, NotFound>> RunMigration(
        string migrationName,
        IMigrationService migrationService,
        CancellationToken cancellationToken)
    {
        await migrationService.MigrateAsync(migrationName, cancellationToken);
        
        return TypedResults.Ok();
    }
}
