using Admitto.AppHost.Extensions.AzureStorage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = DistributedApplication.CreateBuilder(args);
var hostEnvironment = builder.Services.BuildServiceProvider().GetRequiredService<IHostEnvironment>();
var isTestingEnvironment = hostEnvironment.IsEnvironment("Testing");


var postgres = builder.AddPostgres("postgres")
    .WithArgs("-c", "wal_level=logical") // Outbox depends on logical replication
    .WithPgWeb(pgweb =>
    {
        pgweb.WithHostPort(8081);
    })
   .WithLifetime(ContainerLifetime.Persistent);
    
if (!isTestingEnvironment)
{
    // postgres = postgres.WithDataVolume("admitto-postgres-data");
}
    
var postgresdb = postgres.AddDatabase("postgresdb");

var storage = builder.AddAzureStorage("storage")
    .RunAsEmulator(azurite =>
    {
        azurite
            .WithQueuePort(10001)
            .WithLifetime(ContainerLifetime.Persistent);
    });
    
var queues = storage.AddQueues("queues")
    .AddQueue("queue")
    .AddQueue("queue-prio");

var keycloakAdminPassword = builder.AddParameter("KeycloakAdminPassword", secret: true);

// For local development use a stable port for the Keycloak resource (8080 in the preceding example). It can be any port, but it should be stable to avoid issues with browser cookies that will persist OIDC tokens (which include the authority URL, with port) beyond the lifetime of the app host.
var keycloak = builder.AddKeycloak("keycloak", 8080, 
    adminPassword: keycloakAdminPassword)
    .WithRealmImport("./Realms/Admitto.json")
    .WithLifetime(ContainerLifetime.Persistent);

if (!isTestingEnvironment)
{
    keycloak = keycloak.WithDataVolume("admitto-keycloak-data");
}

var apiService = builder.AddProject<Projects.Admitto_Api>("api")
        .WithReference(postgresdb)
        .WithReference(queues)
        .WithReference(keycloak)
        .WaitFor(postgresdb)
        .WaitFor(queues)
        .WaitFor(keycloak);

// TODO Only re-enable when migrations are configurable (for testing)
// var worker = builder.AddProject<Projects.Admitto_Worker>("worker")
//     .WithReference(postgresdb)
//     .WithReference(queues)
//     .WaitFor(postgresdb)
//     .WaitFor(queues);

var authSecret = builder.AddParameter("AuthSecret", true);
var authClientId = builder.AddParameter("AuthClientId");
var authClientSecret = builder.AddParameter("AuthClientSecret", true);
var authIssuer = builder.AddParameter("AuthIssuer");

if (!hostEnvironment.IsEnvironment("Testing"))
{
    builder.AddPnpmApp("admin-ui", "../Admitto.UI.Admin", "dev")
        .WithEnvironment("AUTH_SECRET", authSecret)
        .WithEnvironment("AUTH_KEYCLOAK_ID", authClientId)
        .WithEnvironment("AUTH_KEYCLOAK_SECRET", authClientSecret)
        .WithEnvironment("AUTH_KEYCLOAK_ISSUER", authIssuer)
        .WithHttpEndpoint(3000, isProxied: false) // Use a static port number for OAuth redirect URIs
        .WithExternalHttpEndpoints()
        // .WithReference(keycloak)
        .PublishAsDockerFile();
}

builder.Build().Run();
