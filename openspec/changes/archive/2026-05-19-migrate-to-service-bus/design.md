## Context

The worker process currently consumes messages from an Azure Storage Queue via a polling loop in `AzureStorageQueueProcessor`. The API publishes outbound messages to the same queue via `OutboxMessageSender` using `QueueClient`. Both use the `Azure.Storage.Queues` SDK.

Local development uses Azurite (the Azure Storage emulator) via `Aspire.Hosting.Azure.Storage`. The AppHost registers a custom `AzureQueueCreatorHook` to pre-create the queue in Azurite at startup.

Production infra is declared in `infra/modules/storageAccount.bicep` (includes the queue and a private endpoint, both unused now that private networking is dropped).

An `infra/modules/serviceBus.bicep` already exists from a prior migration attempt but is not wired into `main.bicep`. It targets Standard SKU with private endpoints — both need to change.

## Goals / Non-Goals

**Goals:**
- Replace the polling consumer with a push-based `ServiceBusProcessor`
- Replace `QueueClient` sender with `ServiceBusSender`
- Run the Service Bus emulator locally via Aspire (with MSSQL Server workaround)
- Wire `infra/modules/serviceBus.bicep` (Basic SKU, no private endpoints) into `main.bicep`
- Preserve all existing dispatch behaviour (`QueueMessageDispatcher` is untouched)

**Non-Goals:**
- Topics, subscriptions, or sessions — Basic tier queues only
- Dead-letter monitoring or tooling
- Private networking / VNET integration
- Making Bicep production-perfect (best-effort)

## Decisions

### D1: Service Bus Basic tier
**Decision**: Use Basic SKU.  
**Rationale**: Basic provides standard queues and DLQ for ~$0.05/million operations — sufficient for Admitto's load. Standard/Premium add topics, sessions, and private endpoints that are not needed.  
**Alternative considered**: Standard SKU — adds cost with no benefit at current scale.

### D2: Push-based consumer via `ServiceBusProcessor`
**Decision**: Replace the polling loop with `ServiceBusProcessor` (AMQP long-poll, push delivery).  
**Rationale**: Eliminates up to 5s polling latency; simpler code (no backoff loop); `AutoCompleteMessages = false` gives explicit settlement control matching current behaviour.  
**Settlement**: On success → `CompleteMessageAsync`; on exception → processor auto-abandons (message retried up to max delivery count, then dead-lettered).  
**Alternative considered**: Keeping a polling loop over `ServiceBusReceiver.ReceiveMessageAsync` — possible but forfeits the main benefit of Service Bus.

### D3: CloudEvent encoding — binary mode
**Decision**: Send `new ServiceBusMessage(new BinaryData(cloudEvent.ToJsonBytes()))` and receive with `CloudEvent.Parse(args.Message.Body)`.  
**Rationale**: `ServiceBusReceivedMessage.Body` is already `BinaryData`; `CloudEvent.Parse(BinaryData)` is the correct overload. No base64 encoding/decoding needed (Storage Queue messages were stored as base64 strings).  
**Alternative considered**: JSON string intermediary — unnecessary indirection.

### D4: Emulator — MSSQL Server sidecar workaround
**Decision**: Restore `AzureServiceBusBuilderExtensions.ReplaceEmulatorDatabase()` to swap Aspire's auto-added `sql-edge` container with `mcr.microsoft.com/mssql/server:2019-latest`.  
**Rationale**: SQL Edge is not compatible with newer Linux kernels or ARM macOS (dotnet/aspire#9279, still open). The MSSQL Server swap is confirmed to work and was previously used in this project.  
**Call chain**: `.RunAsEmulator(c => c.WithLifetime(ContainerLifetime.Persistent)).ReplaceEmulatorDatabase()`

### D5: Connection name — `"messaging"`
**Decision**: Use `"messaging"` as the Aspire resource name and DI connection name.  
**Rationale**: Matches the prior Service Bus implementation in this project's git history; consistent with treating it as a generic messaging resource rather than a storage resource.  
**Impact**: Environment variable changes from `ConnectionStrings__queues` to `ConnectionStrings__messaging`.

### D6: Single `ServiceBusSender` + `ServiceBusProcessor` as singletons
**Decision**: Register both as `Singleton` in DI, created from the `ServiceBusClient`.  
**Rationale**: `ServiceBusClient` is thread-safe and designed to be shared; sender and processor are long-lived objects. Consistent with how `QueueClient` was registered.  
**Note**: `ServiceBusProcessor` is registered only in the worker's DI (`AddSharedInfrastructureQueueConsumer`), not in the API.

## Risks / Trade-offs

- **Emulator stability** → The MSSQL Server workaround has been used before and works; the official fix for sql-edge is still pending (aspire#9279). If Aspire changes the container naming convention for the emulator sidecar, the workaround may break silently (no containers, no error). Mitigation: verify emulator starts correctly after the change.
- **Message loss on crash** → Service Bus with `AutoCompleteMessages = false` and explicit `CompleteAsync` gives at-least-once delivery. If the worker crashes after dispatch but before `CompleteAsync`, the message will be retried (same as Storage Queue visibility timeout). No regression.
- **Infra Bicep not validated** → The Bicep is best-effort; the connection string format for Service Bus differs from storage queues (`Endpoint=sb://...` vs `DefaultEndpointsProtocol=...`). App bicep files that pass connection strings to container apps need to reference the correct Service Bus output.

## Migration Plan

1. Update `Directory.Packages.props` and project `.csproj` files (package swap)
2. Restore `AzureServiceBusBuilderExtensions.cs` with `ReplaceEmulatorDatabase`
3. Update `AppHost.cs`: `ConfigureStorageQueues` → `ConfigureServiceBus`
4. Delete `Extensions/AzureStorage/` folder (Azurite queue creation hook)
5. Update `DependencyInjection.cs` (sender + processor registrations)
6. Rewrite `OutboxMessageSender.cs` (QueueClient → ServiceBusSender)
7. Rewrite `AzureStorageQueueProcessor.cs` → `AzureServiceBusQueueProcessor.cs` (push processor)
8. Update `MessageQueueProcessor.cs` (start/stop ServiceBusProcessor)
9. Update `infra/modules/serviceBus.bicep` (Basic SKU, no private endpoints)
10. Wire `serviceBus` module into `infra/main.bicep`

**Rollback**: Revert commits. No data migration needed (queue messages are ephemeral).

## Open Questions

_None — all decisions resolved during exploration._
