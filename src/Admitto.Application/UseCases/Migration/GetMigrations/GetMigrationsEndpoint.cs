namespace Amolenk.Admitto.Application.UseCases.Migration.GetMigrations;

public static class GetMigrationsEndpoint
{
    public static RouteGroupBuilder MapGetMigrations(this RouteGroupBuilder group)
    {
        group
            .MapGet("/", GetMigrations)
            .WithName(nameof(GetMigrations))
            .RequireAuthorization(policy => policy.RequireAdmin());
        
        return group;
    }

    private static ValueTask<GetMigrationsResponse> GetMigrations(
        IMigrationService migrationService,
        CancellationToken cancellationToken)
    {
        var response = new GetMigrationsResponse(
            migrationService.GetSupportedMigrations().ToArray());
        
        return ValueTask.FromResult(response);
    }
}
