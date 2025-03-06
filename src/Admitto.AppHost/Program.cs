var builder = DistributedApplication.CreateBuilder(args);


var cosmos = builder.AddConnectionString("cosmos-db");

var postgres = builder.AddPostgres("postgres")
        .WithArgs("-c", "wal_level=logical") // Outbox depends on logical replication
    // .WithPgWeb()
    // .WithDataVolume("admitto-postgres-data")
    // .WithLifetime(ContainerLifetime.Persistent);
    ;
    
var postgresdb = postgres.AddDatabase("postgresdb");

var apiService = builder.AddProject<Projects.Admitto_Api>("api")
    .WithReference(cosmos)
    .WithReference(postgresdb)
    .WaitFor(postgresdb);

var outboxProcessor = builder.AddProject<Projects.Admitto_Worker>("worker")
    .WithReference(cosmos)
    .WithReference(postgresdb)
    .WaitFor(postgresdb);

builder.Build().Run();
