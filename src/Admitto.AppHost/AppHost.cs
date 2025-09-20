using System.Data.Common;
using System.Net;
using Admitto.AppHost.Extensions.AzureServiceBus;
using Admitto.AppHost.Extensions.AzureStorage;
using Amolenk.Admitto.Application.Jobs;
using Aspire.Hosting.Azure;

var builder = DistributedApplication.CreateBuilder(args);

var postgres = builder.ConfigurePostgres();
var postgresDb = postgres.AddDatabase("admitto-db");
var openFgaDb = postgres.AddDatabase("openfga-db");

// var serviceBus = builder.ConfigureServiceBus();
// serviceBus.AddServiceBusQueue("queue");

var queues = builder.ConfigureStorageQueues();

var openFga = builder.ConfigureOpenFga(openFgaDb);

var apiService = builder.AddProject<Projects.Admitto_Api>("api")
    .WithReference(openFga.GetEndpoint("http")).WaitFor(openFga)
    .WithReference(postgresDb).WaitFor(postgresDb)
    .WithReference(queues).WaitFor(queues);

var worker = builder.AddProject<Projects.Admitto_Worker>("worker")
    .WithReference(openFga.GetEndpoint("http")).WaitFor(openFga)
    .WithReference(postgresDb).WaitFor(postgresDb)
    .WithReference(queues).WaitFor(queues);

var jobRunner = builder.AddProject<Projects.Admitto_JobRunner>("job-runner")
    .WithReference(postgresDb).WaitFor(postgresDb)
    .WithReference(queues).WaitFor(queues)
    .WithHttpCommand(
        path: $"/jobs/{WellKnownJob.SendBulkEmails}/run",
        displayName: "Send bulk emails",
        commandOptions: new HttpCommandOptions()
        {
            Description = "Starts a job for sending scheduled bulk emails.",
            IconName = "Send",
            IsHighlighted = true
        });

if (builder.ExecutionContext.IsRunMode)
{
    var mailDev = builder.ConfigureMailDev();
    var keycloak = builder.ConfigureKeycloak();

    // var migration = builder.AddProject<Projects.Admitto_Migration>("migrate")
    //     .WithArgs("run")
    //     .WithReference(openFga.GetEndpoint("http")).WaitFor(openFga)
    //     .WithReference(postgresDb).WaitFor(postgresDb);

    apiService
        .WithEnvironment("AUTHENTICATION__AUTHORITY", $"{keycloak.GetEndpoint("http")}/realms/admitto")
        .WithEnvironment("AUTHENTICATION__VALIDISSUERS__0", $"{keycloak.GetEndpoint("http")}/realms/admitto")
        .WithUrlForEndpoint(
            "http",
            ep => new ResourceUrlAnnotation
            {
                Url = "/scalar",
                DisplayText = "Scalar",
                DisplayLocation = UrlDisplayLocation.SummaryAndDetails
            });
        // .WaitForCompletion(migration);

        worker
            .WithReference(keycloak).WaitFor(keycloak)
            .WaitFor(mailDev);
        // .WaitForCompletion(migration);

    var adminApp = builder.ConfigureAdminApp();
    adminApp.WithReference(apiService).WaitFor(apiService);
}

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
                    .WithHostPort(8011)
                    .WithPgWeb(pgWeb =>
                    {
                        pgWeb
                            .WithHostPort(8010)
                            .WithLifetime(ContainerLifetime.Persistent);
                    });
            });

        return postgres;
    }

    public static IResourceBuilder<AzureServiceBusResource> ConfigureServiceBus(
        this IDistributedApplicationBuilder builder)
    {
        var serviceBus = builder.AddAzureServiceBus("messaging")
            .RunAsEmulator(configure => { configure.WithLifetime(ContainerLifetime.Persistent); })
            .ReplaceEmulatorDatabase();

        return serviceBus;
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

    public static IResourceBuilder<ContainerResource> ConfigureOpenFga(
        this IDistributedApplicationBuilder builder,
        IResourceBuilder<AzurePostgresFlexibleServerDatabaseResource> openFgaDb)
    {
        // TODO Figure out a way to get the connection string in Key Vault

        var openFga = builder.AddContainer("openfga", "openfga/openfga:latest")
            .WithArgs("run")
            .WithEnvironment("OPENFGA_DATASTORE_ENGINE", "postgres")
            .WithHttpEndpoint(port: builder.ExecutionContext.IsRunMode ? 8000 : 80, targetPort: 8080)
            .WithLifetime(ContainerLifetime.Persistent);

        if (builder.ExecutionContext.IsPublishMode)
        {
            return openFga;
        }

        var initOpenFga = builder.AddContainer("openfga-init", "openfga/openfga:latest")
            .WithArgs("migrate")
            .WithEnvironment("OPENFGA_DATASTORE_ENGINE", "postgres")
            .WithPostgresUriEnvironment("OPENFGA_DATASTORE_URI", openFgaDb.Resource)
            .WithLifetime(ContainerLifetime.Persistent)
            .WaitFor(openFgaDb);

        openFga
            .WithPostgresUriEnvironment("OPENFGA_DATASTORE_URI", openFgaDb.Resource)
            .WaitForCompletion(initOpenFga);

        return openFga;
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
        var authSecret = builder.AddParameter("AuthSecret", true);
        var authClientId = builder.AddParameter("AuthClientId");
        var authClientSecret = builder.AddParameter("AuthClientSecret", true);
        var authIssuer = builder.AddParameter("AuthIssuer");

        var app = builder.AddPnpmApp("admin-ui", "../Admitto.UI.Admin", "dev")
            .WithEnvironment("AUTH_SECRET", authSecret)
            .WithEnvironment("AUTH_KEYCLOAK_ID", authClientId)
            .WithEnvironment("AUTH_KEYCLOAK_SECRET", authClientSecret)
            .WithEnvironment("AUTH_KEYCLOAK_ISSUER", authIssuer)
            .WithHttpEndpoint(3000, isProxied: false) // Use a static port number for OAuth redirect URIs
            .WithExternalHttpEndpoints();

        return app;
    }

    private static IResourceBuilder<T> WithPostgresUriEnvironment<T>(
        this IResourceBuilder<T> builder,
        string name,
        AzurePostgresFlexibleServerDatabaseResource resource)
        where T : IResourceWithEnvironment
    {
        return builder.WithEnvironment(async (context) =>
        {
            var adoConnectionString = await resource.ConnectionStringExpression.GetValueAsync(CancellationToken.None);

            var connectionBuilder = new DbConnectionStringBuilder { ConnectionString = adoConnectionString };

            const string host = "host.docker.internal";
            var port = connectionBuilder["Port"];
            var username = connectionBuilder["Username"];
            var password = WebUtility.UrlEncode(connectionBuilder["Password"].ToString());
            var database = connectionBuilder["Database"];

            var connectionString = $"postgres://{username}:{password}@{host}:{port}/{database}?sslmode=disable";

            context.EnvironmentVariables[name] = connectionString;
        });
    }
}