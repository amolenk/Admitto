namespace Admitto.AppHost.Extensions.Postgres;

public static class PostgresDatabaseBuilderExtensions
{
    public static string GetConnectionString<T>(this IResourceBuilder<T> builder)
        where T : PostgresDatabaseResource
    {
        var postgres = builder.Resource.Parent;
        
        var username = postgres.UserNameParameter?.Value ?? "postgres";
        var password = postgres.PasswordParameter.Value;
        var host = postgres.Name;
        var port = postgres.PrimaryEndpoint.TargetPort;
        var database = builder.Resource.DatabaseName;

        return $"postgres://{username}:{password}@{host}:{port}/{database}?sslmode=disable";
    }
}