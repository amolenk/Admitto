using Amolenk.Admitto.AppHost.Extensions.AzureStorage;
using Amolenk.Admitto.AppHost.Extensions;
using Aspire.Hosting.Azure;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Hosting;

var builder = DistributedApplication.CreateBuilder(args);

// Use a consistent suffix for volume names to avoid conflicts when multiple instances of the Admitto app host
// are run from different folders.
var projectHashSuffix = builder.Configuration["AppHost:Sha256"]![..8].ToLowerInvariant();

var postgres = builder.ConfigurePostgres(projectHashSuffix);
var postgresDb = postgres.AddDatabase("admitto-db");
var quartzDb = postgres.AddDatabase("quartz-db");
var betterAuthDb = postgres.AddDatabase("better-auth-db");

if (builder.Environment.IsEndToEndTesting() || builder.Environment.IsDevelopment())
{
    var queues = builder.ConfigureStorageQueues(projectHashSuffix);
    var keycloak = builder.ConfigureKeycloak(projectHashSuffix);
    var mailDev = builder.ConfigureMailDev();

    var migrations = builder.AddProject<Projects.Admitto_Migrations>("migrations")
        .WithReference(postgresDb).WaitFor(postgresDb)
        .WithReference(quartzDb).WaitFor(quartzDb)
        .WithReference(betterAuthDb).WaitFor(betterAuthDb);

    builder.AddProject<Projects.Admitto_Api>("api")
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
            })
        .WithReference(postgresDb)
        .WithReference(quartzDb)
        .WithReference(keycloak).WaitFor(keycloak) // Idiot
        .WithReference(queues).WaitFor(queues)
        .WaitForCompletion(migrations);

    builder.AddProject<Projects.Admitto_Worker>("worker")
        // Only enable caching in development environment to avoid stale data issues in tests
        .WithEnvironment("CACHING__ENABLED", builder.Environment.IsDevelopment().ToString()) 
        .WithEnvironment(
            "EMAIL__DEFAULTSMTP__HOST",
            ReferenceExpression.Create($"{mailDev.GetEndpoint("smtp").Property(EndpointProperty.Host)}"))
        .WithEnvironment(
            "EMAIL__DEFAULTSMTP__PORT",
            ReferenceExpression.Create($"{mailDev.GetEndpoint("smtp").Property(EndpointProperty.Port)}"))
        .WithReference(postgresDb)
        .WithReference(quartzDb)
        .WithReference(queues).WaitFor(queues)
        .WaitFor(mailDev)
        .WaitForCompletion(migrations);
}

if (builder.Environment.IsDevelopment())
{
    // var adminApp = builder.ConfigureAdminApp();
    // adminApp.WithReference(apiService).WaitFor(apiService);
}

try
{
    builder.Build().Run();
}
catch (AggregateException e) when (e.InnerException is TaskCanceledException)
{
    // Ignore task cancellation exceptions on shutdown. Annoying while debugging unit tests.
}

return;

internal static class Extensions
{
    extension(IDistributedApplicationBuilder builder)
    {
        public IResourceBuilder<PostgresServerResource> ConfigurePostgres(string projectHashSuffix)
        {
            // Use a consistent password to prevent authentication failures when the container is recreated while the data volume
            // persists.
            var postgresPassword = builder.AddParameter("PostgresPassword", value: "admin", secret: true);

            var postgres = builder.AddPostgres("postgres", password: postgresPassword)
                .WithDataVolume("admitto-postgres-" + projectHashSuffix)
                .WithLifetime(ContainerLifetime.Persistent);
            
            if (builder.Environment.IsDevelopment())
            {
                postgres
                    .WithHostPort(15003)
                    .WithPgWeb(pgWeb =>
                    {
                        pgWeb
                            .WithHostPort(15004)
                            .WithLifetime(ContainerLifetime.Persistent);
                    });
            }

            return postgres;
        }

        public IResourceBuilder<AzureQueueStorageResource> ConfigureStorageQueues(string projectHashSuffix)
        {
            var storage = builder.AddAzureStorage("storage")
                .RunAsEmulator(configure =>
                {
                    configure
                        .WithDataVolume("admitto-storage-" + projectHashSuffix)
                        .WithLifetime(ContainerLifetime.Persistent);
                });

            var queues = storage.AddQueues("queues")
                .CreateQueue("queue");

            return queues;
        }

        public IResourceBuilder<ContainerResource> ConfigureMailDev()
        {
            var mailDev = builder.AddContainer("maildev", "maildev/maildev:latest")
                .WithHttpEndpoint(15002, targetPort: 1080)
                .WithEndpoint(name: "smtp", scheme: "smtp", targetPort: 1025, isExternal: true, port: 1025)
                .WithLifetime(ContainerLifetime.Persistent);

            return mailDev;
        }

        public IResourceBuilder<KeycloakResource> ConfigureKeycloak(string projectHashSuffix)
        {
            var keycloakAdminPassword = builder.AddParameter("KeycloakAdminPassword", value: "admin", secret: true);

            // For local development use a stable port for the Keycloak resource.
            // It can be any port, but it should be stable to avoid issues with browser cookies that will persist OIDC
            // tokens (which include the authority URL, with port) beyond the lifetime of the app host.
            var keycloak = builder.AddKeycloak(
                    "keycloak",
                    15001,
                    adminPassword: keycloakAdminPassword)
                .WithRealmImport("./KeycloakConfiguration/AdmittoRealm.json")
                .WithDataVolume("admitto-keycloak-" + projectHashSuffix)
                .WithOtlpExporter()
                .WithLifetime(ContainerLifetime.Persistent);

            return keycloak;
        }

        public IResourceBuilder<NodeAppResource> ConfigureAdminApp()
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
}