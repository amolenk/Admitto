var builder = DistributedApplication.CreateBuilder(args);


var cosmos = builder.AddConnectionString("cosmos-db");


// builder.AddAzureCosmosDB("cosmos-db");


// TODO Aspire 9.1 provides built-in support for the preview version of the Azure Cosmos DB emulator
// builder.AddAzureCosmosDB("cosmos-db")
//     .RunAsEmulator(opt => _ = opt
//             .WithContainerName("cosmosdb-emulator")
//             .WithAnnotation(new ContainerImageAnnotation 
//             {
//                 Registry = "mcr.microsoft.com", 
//                 Image = "cosmosdb/linux/azure-cosmos-emulator", 
//                 Tag = "vnext-preview"
//             })
//             // Health checks don't succeed unless we're using https.
//             .WithEnvironment("PROTOCOL", "https")
//             // Data Explorer
//             .WithHttpEndpoint(targetPort: 1234, port: 1234)
//             // Database engine. Must be 8081, because Dapr retrieves the connection string from the emulator's REST API.
//             .WithHttpsEndpoint(targetPort: 8081, port: 8081) 
//             // Persist data.            
//             .WithVolume("cosmos-db-emulator", "/data", isReadOnly: false)
//             .WithLifetime(ContainerLifetime.Persistent)
//      );

var apiService = builder.AddProject<Projects.Admitto_Api>("api")
    .WithReference(cosmos);

var outboxProcessor = builder.AddProject<Projects.Admitto_OutboxProcessor>("outbox-processor")
    .WithReference(cosmos);

builder.Build().Run();
