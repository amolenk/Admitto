using Admitto.AppHost.Extensions.AzureServiceBus;
using Admitto.AppHost.Extensions.AzureStorage;
using Admitto.AppHost.Extensions.Postgres;
using Amolenk.Admitto.Infrastructure;

var builder = DistributedApplication.CreateBuilder(args);

const bool migrate = true;

// Use a consistent password to prevent authentication failures when the container is recreated while the data volume
// persists.
var postgresPassword = builder.AddParameter("PostgresPassword", true);

var postgres = builder.AddPostgres("postgres", password: postgresPassword)
    .WithPgWeb(pgweb =>
    {
        pgweb.WithLifetime(ContainerLifetime.Persistent);
    })
    .WithDataVolume("admitto-postgres-data")
    .WithLifetime(ContainerLifetime.Persistent);

var postgresdb = postgres.AddDatabase("admitto-db");

var openfgadb = postgres.AddDatabase("openfga-db");

var serviceBus = builder.AddAzureServiceBus("messaging")
    .RunAsEmulator(configure =>
    {
        configure.WithLifetime(ContainerLifetime.Persistent);
    })
    .ReplaceEmulatorDatabase();

serviceBus.AddServiceBusQueue("queue");

var storage = builder.AddAzureStorage("storage")
    .RunAsEmulator(azurite =>
    {
        azurite
            .WithQueuePort(10001) // TODO Must be something else for test
            .WithLifetime(ContainerLifetime.Persistent)
            .WithDataVolume("admitto-azurite-data");
    });

var blobs = storage.AddBlobs("blobs");
blobs.AddBlobContainer("data-protection");
    
var initOpenFga = builder.AddContainer("openfga-init", "openfga/openfga:latest")
    .WithArgs("migrate")
    .WithEnvironment("OPENFGA_DATASTORE_ENGINE", "postgres")
    .WithEnvironment("OPENFGA_DATASTORE_URI", openfgadb.GetConnectionString())
    .WithLifetime(ContainerLifetime.Persistent)
    .WaitFor(openfgadb);

var openFga = builder.AddContainer("openfga", "openfga/openfga:latest")
    .WithArgs("run")
    .WithEnvironment("OPENFGA_DATASTORE_ENGINE", "postgres")
    .WithEnvironment("OPENFGA_DATASTORE_URI", openfgadb.GetConnectionString())
    .WithHttpEndpoint(port: 8000, targetPort: 8080)
    .WithLifetime(ContainerLifetime.Persistent)
    .WaitForCompletion(initOpenFga);

var keycloakAdminPassword = builder.AddParameter("KeycloakAdminPassword", secret: true);

// For local development use a stable port for the Keycloak resource (8080 in the preceding example). It can be any port, but it should be stable to avoid issues with browser cookies that will persist OIDC tokens (which include the authority URL, with port) beyond the lifetime of the app host.
var keycloak = builder.AddKeycloak("keycloak", 8080,
    adminPassword: keycloakAdminPassword)
    .WithRealmImport("./KeycloakConfiguration/AdmittoRealm.json")
    .WithDataVolume("admitto-keycloak-data")
    .WithLifetime(ContainerLifetime.Persistent);

var maildev = builder.AddContainer("maildev", "maildev/maildev:latest")
    .WithHttpEndpoint(targetPort: 1080)
    .WithEndpoint(name: "smtp", scheme: "smtp", targetPort: 1025, isExternal: true, port: 1025)
    .WithLifetime(ContainerLifetime.Persistent);

var worker = builder.AddProject<Projects.Admitto_Worker>("worker")
    .WithReference(openFga.GetEndpoint("http"))
    .WithReference(postgresdb)
    .WithReference(serviceBus)
    .WithReference(keycloak)
    .WithReference(blobs)
    .WaitFor(postgresdb)
    .WaitFor(serviceBus)
    .WaitFor(keycloak)
    .WaitFor(openFga)
    .WaitFor(maildev)
    .WaitFor(blobs);

var apiService = builder.AddProject<Projects.Admitto_Api>("api")
    .WithEnvironment("AUTHENTICATION__AUTHORITY", $"{keycloak.GetEndpoint("http")}/realms/admitto")
    .WithEnvironment("AUTHENTICATION__VALIDISSUERS__0", $"{keycloak.GetEndpoint("http")}/realms/admitto")
    .WithUrlForEndpoint("http", ep => new()
    {
        Url            = "/scalar",
        DisplayText    = "Scalar",
        DisplayLocation = UrlDisplayLocation.SummaryAndDetails
    })
    .WithReference(openFga.GetEndpoint("http"))
    .WithReference(postgresdb)
    .WithReference(serviceBus)
    .WithReference(keycloak)
    .WithReference(blobs)
    .WaitFor(postgresdb)
    .WaitFor(serviceBus)
    .WaitFor(keycloak)
    .WaitFor(openFga)
    .WaitFor(blobs);


   


if (migrate)
{
    var migration = builder.AddProject<Projects.Admitto_Migration>("migrate")
        .WithArgs("run")
//        .WithEnvironment("DOTNET_ENVIRONMENT", builder.Environment.EnvironmentName)
        .WithReference(openFga.GetEndpoint("http"))
        .WithReference(postgresdb)
        .WithReference(blobs)
        .WaitFor(openFga)
        .WaitFor(postgresdb)
        .WaitFor(blobs);

    worker.WaitForCompletion(migration);
    apiService.WaitForCompletion(migration);
}

var authSecret = builder.AddParameter("AuthSecret", true);
var authClientId = builder.AddParameter("AuthClientId");
var authClientSecret = builder.AddParameter("AuthClientSecret", true);
var authIssuer = builder.AddParameter("AuthIssuer");

builder.AddPnpmApp("admin-ui", "../Admitto.UI.Admin", "dev")
    .WithEnvironment("AUTH_SECRET", authSecret)
    .WithEnvironment("AUTH_KEYCLOAK_ID", authClientId)
    .WithEnvironment("AUTH_KEYCLOAK_SECRET", authClientSecret)
    .WithEnvironment("AUTH_KEYCLOAK_ISSUER", authIssuer)
    .WithHttpEndpoint(3000, isProxied: false) // Use a static port number for OAuth redirect URIs
    .WithExternalHttpEndpoints()
    .WithReference(apiService)
    .WaitFor(apiService);
    // .PublishAsDockerFile(); // For deployment

builder.Build().Run();


