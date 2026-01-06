using Admitto.AppHost.Extensions.AzureStorage;
using Aspire.Hosting.Azure;

var builder = DistributedApplication.CreateBuilder(args);

var postgres = builder.ConfigurePostgres();
var postgresDb = postgres.AddDatabase("admitto-db");
var quartzDb = postgres.AddDatabase("quartz-db");
var betterAuthDb = postgres.AddDatabase("better-auth-db");

var queues = builder.ConfigureStorageQueues();

var mailDev = builder.ConfigureMailDev();
var keycloak = builder.ConfigureKeycloak();

var migrations = builder.AddProject<Projects.Admitto_Migrations>("migrations")
    .WithReference(postgresDb).WaitFor(postgresDb)
    .WithReference(quartzDb).WaitFor(quartzDb)
    .WithReference(betterAuthDb).WaitFor(betterAuthDb);

builder.AddProject<Projects.Admitto_Api>("api")
    .WithReference(postgresDb).WaitFor(postgresDb)
    .WithReference(quartzDb).WaitFor(quartzDb)
    .WithReference(queues).WaitFor(queues)
    .WithReference(keycloak)
    .WaitForCompletion(migrations)
    .WithEnvironment(
        "AUTHENTICATION__BEARER__AUTHORITY",
        ReferenceExpression.Create($"{keycloak.GetEndpoint("http")}/realms/admitto"))
    .WithUrlForEndpoint(
        "http",
        ep => new ResourceUrlAnnotation
        {
            Url = "/scalar",
            DisplayText = "Scalar",
            DisplayLocation = UrlDisplayLocation.SummaryAndDetails
        });

builder.AddProject<Projects.Admitto_Worker>("worker")
    .WithReference(postgresDb).WaitFor(postgresDb)
    .WithReference(quartzDb).WaitFor(quartzDb)
    .WithReference(queues).WaitFor(queues)
    .WaitFor(mailDev)
    .WithEnvironment(
        "EMAIL__DEFAULTSMTP__HOST",
        ReferenceExpression.Create($"{mailDev.GetEndpoint("smtp").Property(EndpointProperty.Host)}"))
    .WithEnvironment(
        "EMAIL__DEFAULTSMTP__PORT",
        ReferenceExpression.Create($"{mailDev.GetEndpoint("smtp").Property(EndpointProperty.Port)}"))
    .WaitForCompletion(migrations);

    // var adminApp = builder.ConfigureAdminApp();
    // adminApp.WithReference(apiService).WaitFor(apiService);

builder.Build().Run();
return;

internal static class Extensions
{
    public static IResourceBuilder<AzurePostgresFlexibleServerResource> ConfigurePostgres(
        this IDistributedApplicationBuilder builder)
    {
        var postgres = builder.AddAzurePostgresFlexibleServer("postgres")
            .RunAsContainer(container =>
            {
                container
                    .WithDataVolume("admitto-postgres-data")
                    .WithLifetime(ContainerLifetime.Persistent)
                    .WithHostPort(5432)
                    .WithPgWeb(pgWeb =>
                    {
                        pgWeb
                            .WithHostPort(5433)
                            .WithLifetime(ContainerLifetime.Persistent);
                    });
            });

        return postgres;
    }
    
    public static IResourceBuilder<AzureQueueStorageResource> ConfigureStorageQueues(
        this IDistributedApplicationBuilder builder)
    {
        var storage = builder.AddAzureStorage("storage")
            .RunAsEmulator(configure => { configure.WithLifetime(ContainerLifetime.Persistent); });

        var queues = storage.AddQueues("queues")
            .CreateQueue("queue");

        return queues;
    }
    
    public static IResourceBuilder<ContainerResource> ConfigureMailDev(this IDistributedApplicationBuilder builder)
    {
        var mailDev = builder.AddContainer("maildev", "maildev/maildev:latest")
            .WithHttpEndpoint(targetPort: 1080)
            .WithEndpoint(name: "smtp", scheme: "smtp", targetPort: 1025, isExternal: true, port: 1025)
            .WithLifetime(ContainerLifetime.Persistent);

        return mailDev;
    }

    public static IResourceBuilder<KeycloakResource> ConfigureKeycloak(this IDistributedApplicationBuilder builder)
    {
        var keycloakAdminPassword = builder.AddParameter("KeycloakAdminPassword", secret: true);

        // For local development use a stable port for the Keycloak resource (8080 in the preceding example).
        // It can be any port, but it should be stable to avoid issues with browser cookies that will persist OIDC
        // tokens (which include the authority URL, with port) beyond the lifetime of the app host.
        var keycloak = builder.AddKeycloak(
                "keycloak",
                8080,
                adminPassword: keycloakAdminPassword)
            .WithRealmImport("./KeycloakConfiguration/AdmittoRealm.json")
            .WithDataVolume("admitto-keycloak-data")
            .WithLifetime(ContainerLifetime.Persistent);

        return keycloak;
    }

    public static IResourceBuilder<NodeAppResource> ConfigureAdminApp(this IDistributedApplicationBuilder builder)
    {
        // var authSecret = builder.AddParameter("AuthSecret", true);
        // var authClientId = builder.AddParameter("AuthClientId");
        // var authClientSecret = builder.AddParameter("AuthClientSecret", true);
        // var authIssuer = builder.AddParameter("AuthIssuer");
        //
        // var app = builder.AddPnpmApp("admin-ui", "../Admitto.UI.Admin", "dev")
        //     .WithEnvironment("AUTH_SECRET", authSecret)
        //     .WithEnvironment("AUTH_KEYCLOAK_ID", authClientId)
        //     .WithEnvironment("AUTH_KEYCLOAK_SECRET", authClientSecret)
        //     .WithEnvironment("AUTH_KEYCLOAK_ISSUER", authIssuer)
        //     .WithHttpEndpoint(3000, isProxied: false) // Use a static port number for OAuth redirect URIs
        //     .WithExternalHttpEndpoints();

        throw new NotImplementedException();
    }
}