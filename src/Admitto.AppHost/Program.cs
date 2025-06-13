using Admitto.AppHost.Extensions.AzureStorage;
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

var storage = builder.AddAzureStorage("storage")
    .RunAsEmulator(azurite =>
    {
        azurite
            .WithQueuePort(10001) // TODO Must be something else for test
            .WithLifetime(ContainerLifetime.Persistent);
    });
    
var queues = storage.AddQueues(Constants.AzureQueueStorage.ResourceName)
    .AddQueue(Constants.AzureQueueStorage.DefaultQueueName)
    .AddQueue(Constants.AzureQueueStorage.PrioQueueName);

var initOpenFga = builder.AddContainer("openfga-init", "openfga/openfga:latest")
    .WithArgs("migrate")
    .WithEnvironment("OPENFGA_DATASTORE_ENGINE", "postgres")
    .WithEnvironment("OPENFGA_DATASTORE_URI", GetOpenFgaDatabaseConnectionString)
    .WithLifetime(ContainerLifetime.Persistent)
    .WaitFor(openfgadb);

var openFga = builder.AddContainer("openfga", "openfga/openfga:latest")
    .WithArgs("run")
    .WithEnvironment("OPENFGA_DATASTORE_ENGINE", "postgres")
    .WithEnvironment("OPENFGA_DATASTORE_URI", GetOpenFgaDatabaseConnectionString)
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
    .WithEndpoint(name: "smtp", scheme: "smtp", targetPort: 1025, isExternal: true);

var worker = builder.AddProject<Projects.Admitto_Worker>("worker")
    .WithReference(openFga.GetEndpoint("http"))
    .WithReference(postgresdb)
    .WithReference(queues)
    .WithReference(keycloak)
    // .WithReference(maildev.GetEndpoint("http"))
    .WaitFor(postgresdb)
    .WaitFor(queues)
    .WaitFor(keycloak)
    .WaitFor(openFga)
    .WaitFor(maildev)
    // .WithEnvironment("EMAIL__SMTPSERVER", () => maildev.GetEndpoint("smtp").Host)
    // .WithEnvironment("EMAIL__SMTPPORT", () => maildev.GetEndpoint("smtp").Port.ToString())
    ;

var apiService = builder.AddProject<Projects.Admitto_Api>("api")
    .WithEnvironment("AUTHENTICATION__AUTHORITY", $"{keycloak.GetEndpoint("http")}/realms/admitto")
    .WithReference(openFga.GetEndpoint("http"))
    .WithReference(postgresdb)
    .WithReference(queues)
    .WithReference(keycloak)
    .WaitFor(worker);

if (migrate)
{
    var migration = builder.AddProject<Projects.Admitto_Migration>("migrate")
        .WithArgs("run")
//        .WithEnvironment("DOTNET_ENVIRONMENT", builder.Environment.EnvironmentName)
        .WithReference(openFga.GetEndpoint("http"))
        .WithReference(postgresdb)
        .WaitFor(openFga)
        .WaitFor(postgresdb);

    worker.WaitForCompletion(migration);
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
return;

string GetOpenFgaDatabaseConnectionString()
{
    var username = postgres.Resource.UserNameParameter?.Value ?? "postgres";
    var password = postgres.Resource.PasswordParameter.Value;
    var host = postgres.Resource.Name;
    var port = postgres.Resource.PrimaryEndpoint.TargetPort;
    var database = openfgadb.Resource.DatabaseName;

    return $"postgres://{username}:{password}@{host}:{port}/{database}?sslmode=disable";
}

