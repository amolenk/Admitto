## 1. Package and Project References

- [x] 1.1 Add `Aspire.Hosting.Azure.ServiceBus` (13.2.4), `Aspire.Azure.Messaging.ServiceBus` (13.2.4), and `Azure.Messaging.ServiceBus` (7.20.1) to `Directory.Packages.props`; remove `Aspire.Hosting.Azure.Storage`, `Aspire.Azure.Storage.Queues`, and `Azure.Storage.Queues`
- [x] 1.2 Update `src/Admitto.AppHost/Admitto.AppHost.csproj`: remove `Aspire.Hosting.Azure.Storage` package reference, add `Aspire.Hosting.Azure.ServiceBus`
- [x] 1.3 Update `src/Admitto.Core/Admitto.Core.csproj`: remove `Aspire.Azure.Storage.Queues` and `Azure.Storage.Queues`, add `Aspire.Azure.Messaging.ServiceBus` and `Azure.Messaging.ServiceBus`

## 2. AppHost — Emulator and Orchestration

- [x] 2.1 Restore `src/Admitto.AppHost/Extensions/AzureServiceBus/AzureServiceBusBuilderExtensions.cs` with the `ReplaceEmulatorDatabase()` extension method that swaps the `sql-edge` sidecar container for `mcr.microsoft.com/mssql/server:2019-latest`
- [x] 2.2 Update `AppHost.cs`: replace `ConfigureStorageQueues()` with `ConfigureServiceBus()` using `AddAzureServiceBus("messaging").RunAsEmulator(...).ReplaceEmulatorDatabase()` and `AddQueue("queue")`; update all `.WithReference(queues)` to `.WithReference(serviceBus)` for both `api` and `worker` projects
- [x] 2.3 Delete `src/Admitto.AppHost/Extensions/AzureStorage/` folder (3 files: `AzureQueueAnnotation.cs`, `AzureQueueCreatorHook.cs`, `AzureQueueStorageBuilderExtensions.cs`)

## 3. Core — Dependency Injection

- [x] 3.1 Update `AddSharedInfrastructureMessagingServices()` in `DependencyInjection.cs`: replace `AddAzureQueueServiceClient(connectionName: "queues")` + `QueueClient` singleton with `AddAzureServiceBusClient(connectionName: "messaging")` + `ServiceBusSender` singleton (`client.CreateSender("queue")`)
- [x] 3.2 Update `AddSharedInfrastructureQueueConsumer()` in `DependencyInjection.cs`: add `ServiceBusProcessor` singleton (`client.CreateProcessor("queue", new ServiceBusProcessorOptions { AutoCompleteMessages = false, MaxConcurrentCalls = 1 })`)

## 4. Core — Message Sender

- [x] 4.1 Rewrite `OutboxMessageSender.cs`: inject `ServiceBusSender` instead of `QueueClient`; send with `sender.SendMessageAsync(new ServiceBusMessage(new BinaryData(message.Payload)), cancellationToken)`; update telemetry tag `messaging.system` from `"AzureStorageQueues"` to `"AzureServiceBus"`

## 5. Core — Message Consumer

- [x] 5.1 Replace `AzureStorageQueueProcessor.cs` with `AzureServiceBusQueueProcessor.cs`: thin wrapper that registers `ProcessMessageAsync` and `ProcessErrorAsync` handlers on a `ServiceBusProcessor`; on success calls `CompleteMessageAsync`; on exception lets the processor auto-abandon
- [x] 5.2 Update `MessageQueueProcessor.cs`: inject `ServiceBusProcessor` (via `AzureServiceBusQueueProcessor`); replace polling loop with `processor.StartProcessingAsync` / `StopProcessingAsync`

## 6. Infrastructure (Bicep)

- [x] 6.1 Simplify `infra/modules/serviceBus.bicep`: change SKU from `Standard` to `Basic`; remove all private endpoint, DNS zone, and VNET parameters and resources; keep `disableLocalAuth: true`, queue resource, and `AzureServiceBusDataOwner` role assignment
- [x] 6.2 Wire `serviceBus` module into `infra/main.bicep`: add module block, pass outputs (`serviceBusEndpoint` or connection string) to the API and worker app modules; remove the queue-related outputs from the storage account module

## 7. Verification

- [x] 7.1 Run `dotnet build` to confirm no compilation errors after the package and code changes
- [x] 7.2 Start the stack with `aspire start` and verify the Service Bus emulator container starts (check MSSQL Server sidecar is running, not sql-edge)
- [x] 7.3 Run arch tests: `dotnet test tests/Admitto.Core.ArchTests/Admitto.Core.ArchTests.csproj`
- [x] 7.4 Run integration tests: `dotnet test tests/Admitto.Core.IntegrationTests/Admitto.Core.IntegrationTests.csproj`
